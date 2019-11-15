from typing import List
import numpy as np


class ISource:
    """
    Описывает интерфейс источника
    """

    def configure(self, config:str) -> None:
        """
        Конфигурирует источник данных

        Переопределить метод в классах наследниках
        :param config: str
            Путь к файлу конфигурации источника
        :return: None
        """
        pass

    def run(self) -> None:
        """
        Запускает на выполнение источник данных

        Переопределить метод в классах наследниках
        :return:
        """
        pass