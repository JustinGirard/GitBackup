import argparse
class BaseService:
    @classmethod
    def get_command_map(cls):
        raise Exception("Unimplemented - please override this method")

    @classmethod
    def run(cls, **kwargs):
        command_map = cls.get_command_map()
        assert len(kwargs['__command']) == 1, f"Exactly one command must be specified {kwargs['__command']}"
        cmd = kwargs['__command'][0]
        if cmd not in command_map:
            raise ValueError(f"Unknown command: {cmd}. Commands supported {''.join(command_map)}")

        command_info = command_map[cmd]
        required_args = command_info.get('required_args', [])
        method = command_info['method']
        for arg in required_args:
            assert arg in kwargs, f"Missing required argument: {arg} for command {cmd}"
        del(kwargs['__command'])
        method_kwargs = kwargs
        return method(**method_kwargs)

    @classmethod
    def run_cli(cls):
        parser = argparse.ArgumentParser(description="Generic service CLI for any BaseService subclass.")
        parser.add_argument('args', nargs=argparse.REMAINDER, help='Command arguments and key=value pairs')
        args = parser.parse_args()

        positional_args = []
        kwargs = {}
        for item in args.args:
            if '=' in item:
                key, value = item.split('=', 1)
                kwargs[key] = value
            else:
                positional_args.append(item)

        if positional_args:
            kwargs['__command'] = positional_args

        result = cls.run(**kwargs)
        print(f"{result}")
        #return result