import pickle
import os
import yaml
from typing import List


class Pickler:
    """
    Описывает класс для сохранения/восстановления переменных их файлов
    """

    def __init__(self, exclution_vars: List[str] = [],
                 var_prefix: str = 'gv',
                 store_path: str = "vars/", config_path='config.yaml'):
        self.exclution_vars: List[str] = exclution_vars
        self.var_prefix: str = var_prefix
        self.store_path: str = store_path
        self.config_path: str = config_path

        # зпускает конфигурирование Pickler
        self.configure()

    def configure(self):
        # Загрузка конфигурации
        with open(self.config_path, 'r') as conf:
            self.config = yaml.safe_load(conf)
        # Переменные для исключения из сохранения
        self.exclution_vars = list(set(self.exclution_vars + self.config['exclution_vars']))

    def _do_name_filter(self):
        # Проходим по глобальным переменным и выбираем переменные с префиксом
        variable_names = list(globals().keys()).copy()
        for var in variable_names:
            if var not in self.exclution_vars:  # Если переменная не в списке исклбчения
                if var.startswith(self.var_prefix):  # Если переменная содержит префикс
                    yield var

    def _do_save_type(self, variable_value) -> str:
        return 'pickle'

    def _do_load_type(self, variable_name) -> str:
        return 'pickle'

    def save(self):
        vars_for_save = self._do_name_filter()  # Выберем переменные для сохранения
        for variable_name in vars_for_save:
            variable_value = globals()[variable_name]  # Извлекаем значение переменной
            save_type: str = self._do_save_type(variable_value)  # Определяем тип переменной
            self._save(save_type, variable_name, variable_value)  # Сохраняем переменную

    def _save(self, save_type, variable_name, variable_value):
        new_variable_name = f"{variable_name}_{save_type}"  # Новое имя для переменной
        if save_type == "pickle":
            self._save_pickle(variable_value, new_variable_name)

    def load(self):
        for var_path in os.listdir(self.store_path):
            load_type = self._do_load_type(var_path)
            variable_name = var_path.split('.')[0]
            self._load(load_type, variable_name, var_path)

    def _load(self, load_type, variable_name, variable_path):
        new_variable_name = variable_name.split("_")[0]  # Новое имя для переменной
        if load_type == "pickle":
            self._load_pickle(new_variable_name, variable_path)

    def _save_pickle(self, var, name):
        if not os.path.exists(self.store_path):
            os.makedirs(self.store_path)
        pickle.dump(var, open(f"{self.store_path}{name}{'.pkl'}", "wb"))

    def _load_pickle(self, variable_name, variable_path):
        globals()[variable_name] = pickle.load(open(f"{self.store_path}{variable_path}", "rb"))
