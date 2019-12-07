import pickle
import os


class Pickler:
    def pickle(self, variables: list = [dict(type='pickle', var=None, name='var')], save=True,
               path: str = "vars/") -> object:
        self.path = path
        result = {}
        for var in variables:
            if var['type'] == 'pickle':
                if save:
                    self._save_pickle(var['var'], var['name'])
                else:
                    result[var['name']] = self._load_pickle(var['name'])
        return result if len(result) > 0 else None

    def _save_pickle(self, var, name):
        if not os.path.exists(self.path):
            os.makedirs(self.path)
        pickle.dump(var, open(f"{self.path}{name}{'.pickle'}", "wb"))

    def _load_pickle(self, name):
        return pickle.load(open(f"{self.path}{name}{'.pickle'}", "rb"))
