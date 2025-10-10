import os
from dataclasses import dataclass
from pathlib import Path
from urllib.parse import urlparse

from playwright.sync_api import sync_playwright, TimeoutError as PWTimeout

from tulipService import Bot
from tulipService.model.variableModel import VariableModel
from dmsService import Dms


# ------------ Inputs required for DMS download + AuditBoard upload ------------
@dataclass
class BotInputSchema:
    # EITHER: use DMS (file_path_signature) ...
    dms_download_url: VariableModel = None
    file_path_signature: VariableModel = None
    file_upload_wait: VariableModel = None
    DMS_identity: VariableModel = None
    # OR: use a local path directly
    local_file_path: VariableModel = None

    # AuditBoard page URL pieces
    auditboard_base_url: VariableModel = None
    auditboard_control_code: VariableModel = None

    # Credentials (Tulip identity with basic auth)
    auditboard_identity: VariableModel = None

    # XPaths / selectors
    login_button_xpath: VariableModel = None
    username_xpath: VariableModel = None
    password_xpath: VariableModel = None
    submit_xpath: VariableModel = None
    files_button_xpath: VariableModel = None
    upload_input_xpath: VariableModel = None
    filename_text_xpath: VariableModel = None

    # Optional: dismissor for popups
    cancel_button_xpath: VariableModel = None


@dataclass
class BotOutputSchema:
    def __init__(self):
        ...


class BotLogic(Bot):
    def __init__(self) -> None:
        super().__init__()
        try:
            self.outputs = BotOutputSchema()
            self.input = self.bot_input.get_proposedBotInputs(BotInputs=BotInputSchema)

            # --- Alias shim (supports snake_case or PascalCase keys in input.json)
            def _alias(dst: str, *srcs: str):
                if getattr(self.input, dst, None) is None:
                    for s in srcs:
                        node = getattr(self.input, s, None)
                        if node is not None:
                            setattr(self.input, dst, node)
                            break

            _alias("dms_download_url",       "DmsDownloadUrl")
            _alias("file_upload_wait",       "file_upload_wait")
            _alias("file_path_signature",    "FilePathSignature")
            _alias("local_file_path",        "LocalFilePath", "file_path", "filepath", "FilePath",
                                             "file_to_upload_path", "FileToUploadPath")
            _alias("auditboard_base_url",    "AuditBoardUrl")
            _alias("auditboard_control_code","ControlCode")
            _alias("login_button_xpath",     "LoginButtonXPath")
            _alias("username_xpath",         "UsernameXPath")
            _alias("password_xpath",         "PasswordXPath")
            _alias("submit_xpath",           "SubmitXPath")
            _alias("files_button_xpath",     "FilesButtonXPath")
            _alias("upload_input_xpath",     "UploadFilesXPath")
            _alias("filename_text_xpath",    "FilenameTextXpath")
            _alias("auditboard_identity",    "AuditboardIdentityKey")
            _alias("DMS_identity",           "tulipGenericIdentityKey")

            # Decide between DMS vs local based on provided values
            def _val(node):  # safe .value getter
                return getattr(node, "value", "").strip() if node else ""


            self._sig_val   = _val(getattr(self.input, "file_path_signature", None))
            self._local_val = _val(getattr(self.input, "local_file_path", None))
            self._use_dms   = bool(self._sig_val)  # prefer DMS when signature present

            # Init DMS client only if needed
            if self._use_dms:
                raw = self.input.dms_download_url.value
                parsed = urlparse(raw)
                beekeeper_base = f"{parsed.scheme}://{parsed.netloc}"

                dms_ident = self.bot_input.get_identity(self.input.DMS_identity.value)
                self._dms = Dms(
                    beekeeper_url=beekeeper_base,  # base host only
                    user_name=dms_ident.credential.basicAuth.username,
                    password=dms_ident.credential.basicAuth.password,
                    logger=self.log
                )
                self.log.info("DMS client initialized.")
            else:
                self._dms = None
                self.log.info("No file_path_signature provided; will use local_file_path.")
        except Exception as error:
            self.log.error(f"Error in initiating bot: {error}")

    # ------------------------------ MAIN ------------------------------
    def main(self):
        try:
            workdir = getattr(self, "working_dir", os.getcwd())
            os.makedirs(workdir, exist_ok=True)

            # Choose source file:
            if self._use_dms:
                # 1) Download from DMS to the working directory
                downloaded_path = self._dms.download_file_dms(
                    self._sig_val,
                    save_directory=workdir
                )

                # Fallback: stream & save if strict content-type made the helper return None
                if not downloaded_path or not os.path.isfile(downloaded_path):
                    self.log.info("download_file_dms returned no file; falling back to stream download.")
                    content = self._dms.download_file_stream_dms(self._sig_val)
                    file_name = Path(self._sig_val).name or "downloaded_file"
                    downloaded_path = os.path.join(workdir, file_name)
                    with open(downloaded_path, "wb") as f:
                        f.write(content)
                    self.log.info(f"Saved streamed file to {downloaded_path}")

                source_path = downloaded_path
            else:
                # 2) Use local path from inputs.json
                raw_path = self._local_val
                if not raw_path:
                    raise ValueError("Provide either 'file_path_signature' (DMS) or 'filepath'/'local_file_path' in inputs.json.")

                # Resolve to absolute; allow relative paths relative to working_dir
                source_path = raw_path
                if not os.path.isabs(source_path):
                    source_path = os.path.abspath(os.path.join(workdir, source_path))
                if not os.path.isfile(source_path):
                    raise FileNotFoundError(f"Local file not found: {source_path}")
                self.log.info(f"Using local file: {source_path}")

            # Upload to AuditBoard
            ok = self.upload_file_to_auditboard(file_path=source_path)
            if not ok:
                raise RuntimeError("AuditBoard upload failed or timed out.")

            self.bot_output.success("Source prepared and uploaded to AuditBoard.")
        except Exception as error:
            self.log.error(f"Error in main execution: {error}")
            self.bot_output.error()

    # -------------------------- AuditBoard upload --------------------------
    def upload_file_to_auditboard(self, file_path: str, timeout_sec: int = 30) -> bool:
        """
        Open AuditBoard URL, log in, navigate to Files, upload file, wait for filename to appear.
        """
        if not os.path.isfile(file_path):
            raise FileNotFoundError(f"File to upload not found: {file_path}")

        url = f"{self.input.auditboard_base_url.value}{self.input.auditboard_control_code.value}"
        self.log.info(f"Navigating to: {url}")
        def _sel(selector: str) -> str:
            if selector.startswith(("xpath=", "css=", "text=", "id=")):
                return selector
            if selector.strip().startswith(("/", "(")):    # XPath
                return f"xpath={selector}"
            return selector  # assume CSS

        ident = self.bot_input.get_identity(self.input.auditboard_identity.value)
        user = ident.credential.basicAuth.username
        pwd  = ident.credential.basicAuth.password

        with sync_playwright() as p:
            browser = None
            context = None
            try:

                browser = p.chromium.launch(
                    channel="chrome",  # use the Chrome already on the machine
                    headless=False,
                    args=["--start-maximized"]  # optional: open maximized
                )

                context = browser.new_context(no_viewport=True)
                page = context.new_page()

                page.goto(url, wait_until="domcontentloaded", timeout=timeout_sec * 1000)

                # login
                page.wait_for_selector(_sel(self.input.login_button_xpath.value), state="visible", timeout=timeout_sec * 1000)
                page.locator(_sel(self.input.login_button_xpath.value)).click()

                page.wait_for_selector(_sel(self.input.username_xpath.value), state="visible", timeout=timeout_sec * 1000)
                page.locator(_sel(self.input.username_xpath.value)).fill(user)
                page.locator(_sel(self.input.password_xpath.value)).fill(pwd)

                page.locator(_sel(self.input.submit_xpath.value)).click()
                page.wait_for_load_state("domcontentloaded", timeout=timeout_sec * 1000)

                # optional cancel/dismiss
                if self.input.cancel_button_xpath and getattr(self.input.cancel_button_xpath, "value", ""):
                    try:
                        page.locator(_sel(self.input.cancel_button_xpath.value)).click(timeout=5_000)
                    except PWTimeout:
                        pass
                    except Exception:
                        pass

                # Files
                page.wait_for_selector(_sel(self.input.files_button_xpath.value), state="visible", timeout=timeout_sec * 1000)
                page.locator(_sel(self.input.files_button_xpath.value)).click()

                # Upload
                upload_sel = _sel(self.input.upload_input_xpath.value)
                try:
                    page.locator(upload_sel).set_input_files(file_path)
                except Exception:
                    with page.expect_file_chooser(timeout=timeout_sec * 10000) as fc_info:
                        page.locator(upload_sel).click()
                    fc_info.value.set_files(file_path)


                # Wait for filename to appear
                file_upload_wait = self.input.file_upload_wait.value
                page.wait_for_timeout(float(file_upload_wait))
                file_name = Path(file_path).name
                target_xpath = (getattr(self.input.filename_text_xpath, "value", "") or "").strip()
                if "{fileName}" in target_xpath or "{filename}" in target_xpath:
                    target_xpath = target_xpath.format(fileName=file_name, filename=file_name)
                    page.wait_for_selector(_sel(target_xpath), state="visible", timeout=timeout_sec * 1000)
                else:
                    page.get_by_text(file_name, exact=False).wait_for(timeout=timeout_sec * 1000)

                return True

            except Exception as e:
                self.log.error(f"AuditBoard upload failed: {e}")
                return False
            finally:
                try:
                    if context:
                        context.close()
                finally:
                    if browser:
                        browser.close()
