import yaml
from sources.nature_free import NatureFree
from source import ISource


class Grabber:
    """
    Описывает основной механизм для управления запущеннымми источниками данных
    """

    def configure(self, config: str) -> None:
        """
        Конфигурирует грабер
        :param config: str
            Путь на к файлу конфигурации
        :return: Grabber
             Объект Grabber
        """
        print("Grabber.Info: Конфигурация граббера.")
        try:
            with open(config, 'r') as conf:
                _config = yaml.safe_load(conf)  # Считываем конфиг
                self.sources = _config['sources']  # Источники
                self.configs = _config['configs']  # Конфигурации источников
                return self
        except Exception as exe:
            print(f"Grabber.Error: Ошибка считывания конфигурации: {exe}")
            raise

    def _create_source(self, source_type: str) -> ISource:
        """
        Содает объект источника данных

        :param self: Grabber
        :param source_type: str
            Тип источника данных (nature_free)
        :return: ISource
            Объект источника данных
        """

        sorce_list = dict(
            nature_free=NatureFree()
        )
        # Возвращаем объект источника данных на основе его типа
        source_obj = sorce_list.get(source_type)
        if source_obj == None:
            print("Grabber.Error: Ошибка создания источника: Несуществущее имя источника")
            raise
        return source_obj

    def run(self) -> None:
        """
        Запуск источников данных
        :return: None
        """
        # Список сконфигурированных источников
        _configured_sources = []
        # Конфигурация источников
        for src, conf in zip(self.sources, self.configs):
            try:
                print(f"Grabber.Info: Создание источника: {src}.")
                source = self._create_source(source_type=src)
                print(f"Grabber.Info: Конфигурация источника: {src}.")
                source.configure(config=conf)
                print(f"Grabber.Info: Источник {src} сконфигурирован.")
                _configured_sources.append(source)
            except Exception:
                raise

        # Запуск источников
        for src in _configured_sources:
            try:
                src.run()
            except Exception:
                raise



if __name__ == "__main__":
    # Конфигурация граббера и запуск источников
    try:
        Grabber().configure(config='config.yaml').run()
    except Exception as exe:
        print("Grabber.Error: Ошибка запуска граббера.")
