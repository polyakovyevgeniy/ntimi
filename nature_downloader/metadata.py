import os
import pandas as pd
from datetime import datetime
from typing import List


class Metadata:
    """
    Описывает механизм для сохранения данных
    """

    def save(self, save_type: str = 'csv', params: dict = dict(metadata=[],
                                                                path='metadata.csv',
                                                                method='append', prefix='metadata')):
        if save_type == 'csv':
            self._save_to_csv(**params)

    def _save_to_csv(self, metadata: List[dict] = [],
                    path: str = 'metadata.csv', method: str = 'append',
                    prefix: str = 'metadata') -> None:
        """
        Сохраняет метаданные в формат csv
        :param metadata: List[dict]
            Метедата в формате массива с вложенными словарями
        :param path: str
            Путь к файлу с метаданными
        :param method: str
            Метод добавления записей:
            'append' - прикрепить к существущему файлу (удобно если список статей не большой)
            'new' - сгенерировать новый (удобно когда список статей большой).
                Имя геренируется по переданному пути к файлу. Необходимо
                выше в коде сгенерировать имя для каждой новой порции метадаты
        :param prefix: str
            Префикс имени файла, для разделения между журналами если используется опция 'new'
        :return: None
        """
        # Прикрепление к существубщему файлу
        if method == 'append':
            # Проверим существование файла
            if os.path.exists(path):
                # Открываем для дозаписи файла
                with open(path, 'a') as f:
                    df = pd.DataFrame(metadata)  # генерируем DataFrame
                    df.to_csv(f, header=False, index=None)  # сохраняем в csv
            else:  # файл не существует, создаем его
                pd.DataFrame(metadata).to_csv(path, index=None)
        # Генерация нового файла
        if method == 'new':
            # Создаем новый с новым именем
            raw_path = path.split('/')[0:-1]
            new_path = '/'.join(raw_path) + '/'  + prefix + '__' + '_'.join('_'.join('_'.join('_'.join(str(
                datetime.now()).split('-')).split(':')).split('.')).split(' ')) + '.csv'
            pd.DataFrame(metadata).to_csv(new_path, index=None)
