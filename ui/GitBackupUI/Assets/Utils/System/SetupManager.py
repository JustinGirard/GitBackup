

from BaseService import BaseService
from Stage import Stages

class PythonSetupManager(BaseService):
    def __init__(self,):
        self.stages = {}
        self.register_stage(Stages.DownloadPythonMiniconda())
        self.register_stage(Stages.DownloadIPFS())
        self.register_stage(Stages.DownloadRepo())
        self.register_stage(Stages.SetupVirtualEnv())

    def register_stage(self, stage:Stages.Base):
        self.stages[stage.id] = stage 

    @classmethod
    def get_command_map(cls):
        return {
            'stage':{
                'required_args': ['id','mode'],
                'method': cls.run_stage,
                }
        }
    
    @classmethod
    def run_stage(cls, **kwargs):
        id = kwargs["id"]
        mode = kwargs["mode"]
        manager = PythonSetupManager()
        the_stages = manager.stages.keys()
        assert id in list(manager.stages.keys()), f"Stage id:{id} does not seem to be registered, the stages avail: {''.join(the_stages)}"
        assert mode in ['execute','verify','progress'], f"Command must be 'execute' or 'verify' "
        stage:Stages.Base =  manager.stages[id]
        if mode == "execute":
            return stage.execute(kwargs)            
        if mode == "verify":
            return stage.verify(kwargs)            
        if mode == "progress":
            return stage.progress(kwargs)            



if __name__ == "__main__":
    PythonSetupManager.run_cli()