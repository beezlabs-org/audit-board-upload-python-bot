import argparse
import base64
import json
import os

import pytest

from bot import BotLogic


def check_bot_status(filename=os.path.join(os.getcwd(), 'output', 'output.json')):
    if os.path.exists(filename):
        with open(filename, 'r') as file:
            try:
                data = json.load(file)
                return data.get('runStatus')
            except json.JSONDecodeError as e:
                print(f"Error decoding JSON: {e}")
                return False
    else:
        return False


@pytest.fixture
def mock_args(monkeypatch):
    def encode_json_file_to_base64(file_path=os.path.join(os.getcwd(), 'identity.json')):
        if os.path.exists(file_path):
            try:
                # Read the JSON file
                with open(file_path, 'r') as file:
                    json_data = json.load(file)

                # Serialize the JSON object to a JSON string
                json_string = json.dumps(json_data)

                # Encode the JSON string using base64
                encoded_bytes = base64.b64encode(json_string.encode('ascii'))
                encoded_string = encoded_bytes.decode('ascii')

                return encoded_string

            except Exception as error:
                print(f"An error occurred while encoding JSON to base64: {error}")
                return ""
        else:
            return ""

    def mock_parse_args(*args, **kwargs):
        return argparse.Namespace(
            bot_name='testBot',
            working_dir=os.getcwd(),
            identity=encode_json_file_to_base64(),
            hiveBotId='testHiveBotId',
            executionId='testExecutionId'
        )

    monkeypatch.setattr(argparse.ArgumentParser, 'parse_args', mock_parse_args)


@pytest.fixture
def bot_logic_instance(mock_args):
    return BotLogic()


class TestBotLogic:

    def test_bot(self, bot_logic_instance, mock_args):
        bot_logic_instance.run()
        bot_logic_instance.bot_output.load_proposed_bot_outputs(bot_logic_instance.outputs)
        bot_logic_instance.bot_output.bot_execution()
        status = check_bot_status()
        assert status == "SUCCESSFUL", "Bot execution failed."
