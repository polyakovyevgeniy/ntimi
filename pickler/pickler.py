import pickle
import os
import yaml
from typing import List


class Pickler:
    """
    Описывает класс для сохранения/восстановления переменных
    """

    def __init__(self, context: object,  exclution_vars: List[str] = [],
                 var_prefix: str = 'gv',
                 store_path: str = "vars/", config_path='pickler/config.yaml'):

        self.context = context
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
        variable_names = list(self.context.keys()).copy()
        for var in variable_names:
            if var not in self.exclution_vars:  # Если переменная не в списке исклбчения
                if var.startswith(self.var_prefix):  # Если переменная содержит префикс
                    yield var

    def _do_save_type(self, variable_value) -> str:
        return 'pickle'

    def _do_load_type(self, variable_name) -> str:
        return 'pickle'

    def save(self, silent: bool = True):
        vars_for_save = list(self._do_name_filter())  # Выберем переменные для сохранения
        for variable_name in vars_for_save:
            variable_value = self.context[variable_name]  # Извлекаем значение переменной
            save_type: str = self._do_save_type(variable_value)  # Определяем тип переменной
            self._save(save_type, variable_name, variable_value)  # Сохраняем переменную
        if not silent:
            print(f"Saved {len(vars_for_save)} variables.")

    def _save(self, save_type, variable_name, variable_value):
        new_variable_name = f"{variable_name}_{save_type}"  # Новое имя для переменной
        if save_type == "pickle":
            self._save_pickle(variable_value, new_variable_name)

    def load(self, silent: bool = True):
        loaded_vars = {}
        for var_path in os.listdir(self.store_path):
            load_type = self._do_load_type(var_path)
            variable_name = var_path.split('.')[0]
            new_variable_name = variable_name.split("_")[0]  # Новое имя для переменной
            loaded_vars[new_variable_name] = self._load(load_type, new_variable_name, var_path)
        if not silent:
            print(f"Loaded {len(loaded_vars)} variables.")
        return loaded_vars

    def _load(self, load_type: str, variable_name, variable_path) -> object:
        """
        Восстанавливает переменные из файла
        @param load_type: str
            Тип сохраненной переменной(извлекается из имени файла)
        @param variable_name: str
            Имя переменной
        @param variable_path: str
            Путь к переменной
        @return: object
            Объект загруженной переменной
        """
        if load_type == "pickle":
            return self._load_pickle(variable_name, variable_path)

    def get_variables(self, silent: bool = False)-> List[str]:
        """
        Возвращает список переменных которые будут сериализованы
        @param silent: bool
            Флаг, показывать сообщение с количеством переменных или нет
        @return: List[str]
            Список переменных
        """
        list_vars = list(self._do_name_filter())
        if not silent:
            print(f"Variables Count: {len(list_vars)}")
        return list_vars

    def _save_pickle(self, var: object, name: str):
        """
        Сохраняет переменную в pickle файл
        @param var: object
            Значение переменной
        @param name: str
            Имя переменной
        @return: None
        """
        if not os.path.exists(self.store_path):
            os.makedirs(self.store_path)
        pickle.dump(var, open(f"{self.store_path}{name}{'.pkl'}", "wb"))

    def _load_pickle(self, variable_name, variable_path):
        return pickle.load(open(f"{self.store_path}{variable_path}", "rb"))
