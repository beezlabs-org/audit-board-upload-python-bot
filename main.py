from bot import BotLogic

if __name__ == '__main__':
    main = BotLogic()
    main.run()
    main.bot_output.load_proposed_bot_outputs(main.outputs)
    main.bot_output.bot_execution()
