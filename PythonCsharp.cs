using Beezlabs.RPAHive.Lib;
using Beezlabs.RPAHive.Lib.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Beezlabs.RPA.Bots
{
    public class otc012amexicanexceptionbot : RPABotTemplate
    {
        BotExecutionModel botExecutionModel = null;
        BotInputs BotInputs = new BotInputs();
        //VMLoginDetails VMLoginDetails = new VMLoginDetails();
        //OauthCredentials OauthCredentials = new OauthCredentials();
        string pyErrorMsg = "";
        string workingDirectory = "";
        string pythonException = "";
                
        protected override void BotLogic(BotExecutionModel botExecutionModel)
        {
            try
            {
                this.botExecutionModel = botExecutionModel;
                workingDirectory = GetWorkingDirectory();
                //GetInputs();
                //GetVMLoginredentials();
                //GetOauthCredentials();
                GetBotLibraryPath();
                string encodeIdentity = EncodeIdentity();
                CreateInputJson();
                //ConnectToRdp(GetAccessToken());
                Python(encodeIdentity);    
                SendOutputToState();
                //CloseRdp(GetAccessToken());
                Success("Bot Executed Successfully");
            }
            catch (Exception exception)
            {
                LogMessage(this.GetType().Name, $"Bot failed {exception.Message}");
                Failure($"Bot failed : {exception.Message}");
            }
        }

        //private void GetInputs()
        //{
        //    try
        //    {
        //        BotInputs.HexId = GetStringInputs("hexId");
        //        BotInputs.BotClassName = GetStringInputs("botClassName");
        //        BotInputs.ServerName = GetStringInputs("serverName");
        //        BotInputs.ServerAddress = GetStringInputs("serverAddress");
        //        BotInputs.CloseRDPApiUrl = GetStringInputs("closeRDPApiUrl");
        //        BotInputs.ConnectRDPApiUrl = GetStringInputs("connectRDPApiUrl");
        //        BotInputs.AccessTokenBaseurl = GetStringInputs("accessTokenBaseurl");
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Input Error: " + ex.Message);
        //    }
        //}

        //private String GetStringInputs(String key)
        //{
        //    try
        //    {
        //        VariableModel varInput = null;
        //        varInput = null;
        //        LogMessage(this.GetType().Name, "Trying to Input with Key: " + key);

        //        this.botExecutionModel.proposedBotInputs.TryGetValue(key, out varInput);

        //        if (varInput == null || varInput.value == null)
        //        {
        //            throw new Exception("Input not found with key: " + key);
        //        }
        //        return varInput.value.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Input Error with kery {key} : " + ex.Message);
        //    }
        //}

        //private void GetVMLoginredentials()
        //{
        //    try
        //    {
        //        BotInputs.LoginIdentityKey = GetStringInputs("LoginIdentityKey");
        //        LogMessage(this.GetType().FullName, $"VM login Credential key received - {BotInputs.LoginIdentityKey}");
        //        BotIdentityModel vmLgoinCred = this.botExecutionModel.identityList.Find(cred => cred.name.Equals(BotInputs.LoginIdentityKey));
        //        if (vmLgoinCred == null || vmLgoinCred.credential == null || vmLgoinCred.credential.basicAuth == null)
        //        {
        //            throw new Exception("VM login credentials invalid");
        //        }
        //        this.VMLoginDetails = new VMLoginDetails
        //        {
        //            Username = vmLgoinCred.credential.basicAuth.username,
        //            Password = vmLgoinCred.credential.basicAuth.password
        //        };
        //        LogMessage(this.GetType().FullName, "VM login Username and password is passed");
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("VM login credentials not passed " + ex.Message);
        //    }
        //}
        //private void GetOauthCredentials()
        //{
        //    try
        //    {
        //        BotInputs.RDPIdentityKey = GetStringInputs("RDPIdentityKey");
        //        BotIdentityModel rdpCred = this.botExecutionModel.identityList.Find(cred => cred.name.Equals(BotInputs.RDPIdentityKey));
        //        OauthCredentials = new OauthCredentials
        //        {
        //            GrantType = rdpCred.credential.oAuth2.grantType,
        //            ClientId = rdpCred.credential.oAuth2.clientId,
        //            ClientSecret = rdpCred.credential.oAuth2.clientSecret,
        //            Scope = rdpCred.credential.oAuth2.scope,
        //            State = rdpCred.credential.oAuth2.state
        //        };
        //        LogMessage(this.GetType().FullName, "OAuth client id and password is passed");
        //    }
        //    catch (Exception ex)
        //    {
        //        LogMessage(this.GetType().FullName, "OAuth credentials not passed " + ex.Message);
        //        throw new Exception("OAuth credentials not passed " + ex.Message);
        //    }
        //}

        //private void GetOauthCredentials()
        //{
        //    try
        //    {
        //        BotInputs.RDPIdentityKey = GetStringInputs("RDPIdentityKey");
        //        this.OauthCredentials = new OauthCredentials
        //        {
        //            GrantType = this.botExecutionModel.identityList[0].credential.oAuth2.grantType,
        //            ClientId = this.botExecutionModel.identityList[0].credential.oAuth2.clientId,
        //            ClientSecret = this.botExecutionModel.identityList[0].credential.oAuth2.clientSecret,
        //            Scope = this.botExecutionModel.identityList[0].credential.oAuth2.scope,
        //            State = this.botExecutionModel.identityList[0].credential.oAuth2.state
        //        };
        //        LogMessage(this.GetType().FullName, "OAuth client id and password is passed");
        //    }
        //    catch (Exception ex)
        //    {
        //        LogMessage(this.GetType().FullName, "OAuth credentials not passed " + ex.Message);
        //        throw new Exception("OAuth credentials not passed " + ex.Message);
        //    }
        //}

        private void GetBotLibraryPath()
        {
            string assemplyLocation = "";

            MethodBase method = new StackTrace().GetFrame(0).GetMethod();

            // Get the type of the current executing method
            Type type = method.DeclaringType;

            // Get the name of the current executing class
            string className = type.Name;
            // Get all loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.ExportedTypes.ToList().Count > 0 && assembly.ExportedTypes.ToList()[0].Name == className)
                {
                    assemplyLocation = assembly.Location;
                    break;
                }
            }
            string directoryPath = Path.GetDirectoryName(assemplyLocation);
            LogMessage(this.GetType().Name, $"Bot library directory path {directoryPath}");
            BotInputs.BotFilepath = directoryPath;
        }

        private string EncodeIdentity()
        {
            try
            {
                if (botExecutionModel.identityList.Count > 0)
                {
                    string identityJson = System.Text.Json.JsonSerializer.Serialize(this.botExecutionModel.identityList);
                    string encodedIdentity = Convert.ToBase64String(Encoding.ASCII.GetBytes(identityJson));
                    return encodedIdentity;
                }
                else
                    return " ";
            }
            catch
            {
                throw;
            }
        }

        private void Python(string identity)
        {
            try
            {
                MethodBase method = new StackTrace().GetFrame(0).GetMethod();
                Type type = method.DeclaringType;
                string className = type.Name;
                string pythonInterpreter = Path.Combine(BotInputs.BotFilepath, ".venv", "Scripts", "python.exe");

                // Path to setup.py
                string mainPath = Path.Combine(BotInputs.BotFilepath, "main.py");
                LogMessage(this.GetType().Name, $"Setup file path : {mainPath}");
                string hivebotId = botExecutionModel.hiveBotId.ToString();
                string executionId = botExecutionModel.executionId.ToString();
                if (File.Exists(mainPath))
                {
                    // Command to execute
                    string command = $"{@pythonInterpreter} \"{mainPath}\" --bot_name {className} --hiveBotId \"{hivebotId}\" --executionId \"{executionId}\" --working_dir \"{workingDirectory}\" --identity \"{identity}\"";

                    // Start the process
                    Process process = new Process();
                    process.StartInfo.FileName = "cmd.exe"; // Command prompt
                    process.StartInfo.Arguments = $"/c {command}"; // Pass the command
                    process.StartInfo.UseShellExecute = true;

                    // Start the process
                    process.Start();

                    // Wait for the process to exit
                    process.WaitForExit();
                }
                else
                {
                    LogMessage(this.GetType().Name, $"main.py file not found in the path {mainPath}");
                    throw new Exception($"main.py file not found {mainPath}");
                }
            }
            catch (Exception exception)
            {
                LogMessage(this.GetType().Name, $"Error while executing python file {exception.Message}");
                throw;
            }
        }

        private void CreateInputJson()
        {
            try
            {
                JObject root = new JObject();
                JObject botInputs = new JObject();

                foreach (var proposedBotInput in this.botExecutionModel.proposedBotInputs)
                {
                    string key = proposedBotInput.Key;
                    VariableModel variable = proposedBotInput.Value;
                    JToken valueToken;

                    if (variable.value == null || variable.value.Equals(""))
                    {
                        valueToken = "";
                    }
                    else if ((variable.value.GetType().Name.Equals(typeof(JArray).Name) || variable.value.GetType().IsArray) && !(variable.value is string))
                    {
                        string str = variable.value.ToString();
                        valueToken = JArray.Parse(str);
                    }
                    else if (variable.value is IEnumerable<object> enumerable && !(variable.value is string))
                    {
                        string str = enumerable.ToString();
                        valueToken = JObject.Parse(str);
                    }
                    else if (int.TryParse(variable.value.ToString(), out int intValue))
                    {
                        valueToken = intValue;
                    }
                    else if (bool.TryParse(variable.value.ToString(), out bool boolValue))
                    {                        
                        valueToken = boolValue;
                    }
                    else {
                        //check if variable.value is ajson string
                        try
                        {
                            valueToken = JToken.FromObject(variable.value.ToString());
                        }
                        catch
                        {
                            valueToken = variable.value.ToString();
                        }
                    }
                    
                    JObject variableObject = new JObject
                    {
                        { "value", valueToken },
                        { "type", variable.type.ToString() },
                        { "objectTypeName", variable.objectTypeName },
                        { "flowVariable", variable.flowVariable }
                    };
                    botInputs.Add(key, variableObject);
                }

                root.Add("hiveBotId", JToken.FromObject(this.botExecutionModel.hiveBotId));
                root.Add("executionId", JToken.FromObject(this.botExecutionModel.executionId));
                this.botExecutionModel.identityList.Clear();
                root.Add("identityList", JToken.FromObject(this.botExecutionModel.identityList));
                root.Add("connectionParams", "");
                root.Add("proposedBotInputs", botInputs);

                string jsonString = root.ToString(Formatting.Indented);
                LogMessage(this.GetType().Name, $"input.json file's json string created successfully : {jsonString}.");
                string inputDirectory = Path.Combine(workingDirectory, "input");
                if (!Directory.Exists(inputDirectory))
                {
                    Directory.CreateDirectory(inputDirectory);
                    LogMessage(this.GetType().Name, $"input directory created");
                }
                string filePath = Path.Combine(inputDirectory, "input.json");
                File.WriteAllText(filePath, jsonString);
                LogMessage(this.GetType().Name, $"input.json file created successfully in the filepath of {workingDirectory}.");
            }
            catch (Exception exception)
            {
                LogMessage(this.GetType().Name, $"Error while preparing input json file");
                throw;
            }
        }

        private void SendOutputToState()
        {
            try
            {
                LogMessage(this.GetType().Name, $"started to Deserialize the output json file");
                string jsonFilePath = Path.Combine(workingDirectory, "output/output.json"); // assuming the file is named output.json in the current directory
                LogMessage(this.GetType().Name, $"output file path {jsonFilePath}");
                if (File.Exists(jsonFilePath))
                {
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    BotReplyModel botReplyModel = JsonConvert.DeserializeObject<BotReplyModel>(jsonContent);
                    if (botReplyModel.runStatus == RunStatus.SUCCESSFUL)
                    {
                        foreach (var variableModel in botReplyModel.variableMap)
                        {
                            LogMessage(this.GetType().Name, $"key : {variableModel.Key} and value : {variableModel.Value.value}");
                            AddVariable(variableModel.Key, variableModel.Value.type == VariableTypes.OBJECT ? variableModel.Value.value as JToken : variableModel.Value.value);
                        }
                    }
                    else
                    {
                        throw new Exception($"Exception from python script : {botReplyModel.botMessage}");
                    }
                }
                else
                {
                    LogMessage(this.GetType().Name, $"Output file not found");
                    throw new Exception($"Output file not found and the error from python file is : {pythonException}");
                }
            }
            catch (Exception exception)
            {
                LogMessage(this.GetType().Name, $"Error while send outputs to state {exception.Message}");
                throw exception;
            }
        }

        private void AddVariables(string key, object value)
        {
            AddVariable(key, value);
        }

        //private string GetAccessToken()
        //{
        //    try
        //    {
        //        string requestBody = "grant_type=" + Uri.EscapeDataString(OauthCredentials.GrantType.ToString()) +
        //            "&scope=" + Uri.EscapeDataString(OauthCredentials.Scope);

        //        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, BotInputs.AccessTokenBaseurl);

        //        string credentials = OauthCredentials.ClientId + ":" + OauthCredentials.ClientSecret;
        //        string credentialsBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));

        //        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentialsBase64);
        //        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

        //        HttpClient client = new HttpClient();
        //        HttpResponseMessage response = client.SendAsync(request).Result;

        //        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        //        {
        //            var responseString = response.Content.ReadAsStringAsync().Result;
        //            var responseMap = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);
        //            LogMessage(this.GetType().Name, "Access token fetched successfully");
        //            return responseMap["access_token"].ToString();
        //        }
        //        else
        //        {
        //            LogMessage(this.GetType().Name, "Failed to get access token - " + response.Content.ReadAsStringAsync().Result);
        //            throw new Exception("Failed to get access token");
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        LogMessage(this.GetType().Name, "An error occurred while fetching access token - " + exception.Message);
        //        throw new Exception("An error occurred while fetching access token. Exception: " + exception.Message);
        //    }
        //}

    //    private void ConnectToRdp(string accessToken)
    //    {
    //        try
    //        {
    //            LogMessage(this.GetType().Name, "Connecting to RDP");
    //            HttpClientHandler httpClientHandler = new HttpClientHandler
    //            {
    //                ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
    //            };

    //            using (HttpClient client = new HttpClient(httpClientHandler))
    //            {
    //                string requestBody = "{\"Username\":\"" + VMLoginDetails.Username + "\",\"Password\":\"" + VMLoginDetails.Password + "\",\"ServerAddress\":\"" + BotInputs.ServerAddress + "\",\"BotClassName\":\"" + BotInputs.BotClassName + "\",\"ServerName\":\"" + BotInputs.ServerName + "\",\"HexId\":\"" + BotInputs.HexId + "\"}";
    //                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, BotInputs.ConnectRDPApiUrl);
    //                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    //                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

    //                HttpResponseMessage response = client.SendAsync(request).Result;
    //                if (response.StatusCode == HttpStatusCode.OK)
    //                {
    //                    LogMessage(this.GetType().Name, "RDP connection successful");
    //                }
    //                else
    //                {
    //                    LogMessage(this.GetType().Name, "Failed to connect to RDP " + response.Content.ReadAsStringAsync().Result);
    //                    throw new Exception("Failed to connect to RDP");
    //                }
    //            }
    //        }
    //        catch (Exception exception)
    //        {
    //            LogMessage(this.GetType().Name, "while connecting to RDP " + exception.Message);
    //            throw new Exception("while connecting to RDP " + exception.Message + " " + string.Join(", ", exception.StackTrace));
    //        }
    //    }

    //    private void CloseRdp(string accessToken)
    //    {
    //        try
    //        {
    //            LogMessage(this.GetType().Name, "Connecting to RDP");
    //            HttpClientHandler httpClientHandler = new HttpClientHandler
    //            {
    //                ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
    //            };

    //            using (HttpClient client = new HttpClient(httpClientHandler))
    //            {
    //                string requestBody = "{\"RDPServerAddress\":\"" + BotInputs.ServerAddress + "\"}";
    //                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, BotInputs.CloseRDPApiUrl);
    //                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    //                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

    //                HttpResponseMessage response = client.SendAsync(request).Result;
    //                if (response.StatusCode == HttpStatusCode.OK)
    //                {
    //                    LogMessage(this.GetType().Name, "RDP connection successful");
    //                }
    //                else
    //                {
    //                    LogMessage(this.GetType().Name, "Failed to connect to RDP - " + response.Content.ReadAsStringAsync().Result.ToString());
    //                    throw new Exception("Failed to connect to RDP - " + response.Content.ReadAsStringAsync().Result.ToString());
    //                }
    //            }
    //        }
    //        catch (Exception exception)
    //        {
    //            throw new Exception("while connecting to RDP " + exception.Message + " " + string.Join(", ", exception.StackTrace));
    //        }
    //    }
    }

    internal class BotInputs
    {
        //public string HexId { get; set; }
        //public string ServerName { get; set; }
        //public string BotClassName { get; set; }
        //public string ServerAddress { get; set; }
        //public string CloseRDPApiUrl { get; set; }
        //public string ConnectRDPApiUrl { get; set; }
        //public string AccessTokenBaseurl { get; set; }
        //public string RDPIdentityKey { get; set; }
        //public string LoginIdentityKey { get; set; }
        public string BotFilepath { get; set; }
    }
    //class VMLoginDetails
    //{
    //    public string Username { get; set; }
    //    public string Password { get; set; }
    //}
    //class OauthCredentials
    //{
    //    public OAuth2Model.GrantType GrantType { get; set; }
    //    public string ClientId { get; set; }
    //    public string ClientSecret { get; set; }
    //    public string Scope { get; set; }
    //    public string State { get; set; }
    //}
}
