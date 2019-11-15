from source import ISource
import yaml
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
import time
from bs4 import BeautifulSoup
from metadata import Metadata
from datetime import datetime
from typing import List
import os

class NatureFree(ISource):
    """
    Описывает источник для скачивания бесплатых статей с nature.com
    """

    def configure(self, config: str) -> None:
        """
        Конфигурирует источник
        :param config: str
            Путь к файлу конфигурации
        :return: None
        """
        try:
            # Считываем конфигурацию
            with open(config, 'r') as conf:
                _config = yaml.safe_load(conf)
                self.config = _config

            # Проверка конфигурацииы
            self._check_conifigure()
        except Exception as exe:
            print(f"ISource.Info: {exe}")
            raise

    def _check_conifigure(self):
        for config_keys in ['base_res_url', 'base_list_url',
                            'journal_sub_url', 'pagers', 'names',
                            'download_paths', 'driver', 'save_to', 'metadata',
                            'article_delay', 'journal_delay', 'show_browser',
                            'metadata_method']:
            if self.config.get(config_keys) == None:
                raise ('Ошибка конфигурации')

    def _create_driver(self):
        # Опции для браузера
        try:
            options = Options()
            if self.config['show_browser'] == "false":
                options.add_argument("--headless")
            options.add_experimental_option('prefs', {
                "download.prompt_for_download": False,
                "plugins.always_open_pdf_externally": True
            })
        except Exception as exe:
            raise (exe)

        self.driver = webdriver.Chrome(executable_path=self.config['driver'],
                                       options=options)

    def _get_range(self, pager: str)-> List[int]:
        """
        Возвращает список страниц из интервала указанного в конфиге
        :param pager:
        :return:
        """
        pages_tmp = []
        # Разбиваем на запятые
        variant_split = pager.split(',')
        for vs in variant_split:
            rng_split = vs.split('-')
            if len(rng_split) > 1:
                pages_tmp.extend(list(range(int(rng_split[0]), int(rng_split[1]) + 1)))
            else:
                pages_tmp.append(int(rng_split[0]))
        return sorted(pages_tmp)

    def _get_metadata(self, article_url, journal_index):
        # Извлекает метаданные из странички
        # Получаем страничку
        self.driver.get(article_url)
        # Переводим в HTML
        html = BeautifulSoup(self.driver.page_source, 'lxml')

        # Заголовок статьи
        article_title = self._get_article_title(html=html)
        if article_title == None:
            raise
        # Тип статьи
        article_type = self._get_atricle_type(html=html)
        if article_type == None:
            article_type = 'None'
        # Дата публикации
        publish_date = self._get_publish_date(html=html)
        if publish_date == None:
            publish_date == 'None'
        # Авторы
        authors = self._get_authors(html=html)
        if authors == None:
            authors = 'None'
        # Аннотация
        abstract = self._get_abstract(html=html)
        if abstract == None:
            abstract = 'None'
        # Аффиляция
        affilations = self._get_affilations(html=html)
        if affilations == None:
            affilations = 'None'
        # Аффиляция по авторам
        aff_authors = self._get_authors_affilations(html=html)
        if aff_authors == None:
            aff_authors = 'None'

        # Библио инфо
        bib_info = self._get_biblio_info(html=html)
        if bib_info == None:
            bib_info = 'None'

        subjects = self._get_subjects(html=html)
        if subjects == None:
            subjects = 'None'

        # Текст статьи
        text = self._get_text(html=html)
        if text == None:
            text = 'None'

        return [dict(create_date=datetime.now(),
                     journal_name=self.config['names'][journal_index],
                     publish_date=publish_date, type=article_type, title=article_title,
                     authors=authors, abstract=abstract,
                     affilations=affilations, aff_authors=aff_authors,
                     subjects=subjects, biblio=bib_info,
                     text=text,
                     file_path=f"/{self.config['names'][journal_index]}/{article_url.split('/')[-1]}.pdf")]


    def _get_text(self, html: BeautifulSoup) -> List[str]:
        """
        Возврашает текст статьи
        :param html:
        :return:
        """
        try:
            text_arr = html.find_all("div", {'c-article-section__content'})
            txt_res_arr = []
            for text in text_arr:
                if str(text['id']).startswith('Sec'):
                    txt_res_arr.append(text.text)
            return txt_res_arr
        except Exception as exe:
            print(f"ISource.Error: Ошибка получения текста статьи: {exe}")
            return None

    def _get_subjects(self, html: BeautifulSoup):
        """
        Возвращает информацию по тематикам
        :param html:
        :return:
        """
        try:
            return [sub.text for sub in html.find_all("li",
                                               {'c-article-subject-list__subject'})]
        except Exception as exe:
            print(f"ISource.Error: Ошибка получения информации по тематикам: {exe}")
            return None


    def _get_biblio_info(self, html: BeautifulSoup) -> List[str]:
        """
        Возвращает информацию по библиографии
        :param html:
        :return:
        """
        try:
            return [bib.text for bib in html.find_all("li",
                                               {'c-bibliographic-information__list-item'})]
        except Exception as exe:
            print(f"ISource.Error: Ошибка получения информации по библиографии: {exe}")
            return None

    def _get_authors_affilations(self, html: BeautifulSoup) -> List[str]:
        """
        Возвращает авторов по аффиляции
        :param html:
        :return:
        """
        try:
            return [ul.text for ul in html.find_all("ul",
                                                       {"c-article-author-affiliation__authors-list"})]
        except Exception as exe:
            print(f"ISource.Error: Ошибка получения авторов для аффиляции: {exe}")
            return None


    def _get_affilations(self, html: BeautifulSoup) -> List[str]:
        """
        Возвращает аффиляцию для статьи
        :param html: BeautifulSoup
            html разметка в виде класса BeautifulSoup
        :return: List[str]
            Аффиляция для авторов
        """
        try:
            return [aff.text for aff in html.find_all("h4",
                   {"c-article-author-affiliation__address u-h3"})]
        except Exception as exe:
            print(f"ISource.Error: Ошибка получения аффиляции статьи: {exe}")
            return None

    def _save_metadata(self, metadata_info, journal_index:int):
        Metadata().save(save_type=self.config['save_to'],
                        params=dict(
                        metadata=metadata_info,
                               path=os.path.join(self.config['metadata'][self.config['save_to']],
                                                 self.config['names'][journal_index] + '.csv'),
                               method=self.config['metadata_method'][journal_index],
                            prefix=self.config['names'][journal_index]))

    def _get_article_title(self, html: BeautifulSoup) -> str:
        """
        Возвращает заголовок статьи
        :param html:
        :return:
        """
        try:
            return html.find("h1", {"c-article-title u-h1"}).text
        except Exception as exe:
            print(f"ISource.Error: Ошибка получения заголовка статьи: {exe}")
            return None

    def _get_authors(self, html: BeautifulSoup) -> List[str]:
        """
        Возвращает авторов статьи
        :param html:
            html разметка в виде класса BeautifulSoup
        :return: List[str]
            Список авторов
        """
        try:
            return [a.text for a in html.find_all("a", {"data-test": "author-name"})]
        except Exception as exe:
            print(f"ISource.Error: Ошибка получения авторов статьи: {exe}")
            return None

    def _get_abstract(self, html: BeautifulSoup) -> str:
        """
        Возвращает аннотацию статьи
        :param html:
            html разметка в виде класса BeautifulSoup
        :return: str
            Аннотация
        """
        try:
            return html.find("div", {"c-article-section__content"}).text
        except Exception as exe:
            print(f"ISource.Error: Ошибка получения аннотации статьи: {exe}")
            return None

    def _get_publish_date(self, html: BeautifulSoup) -> str:
        """
        Возвращает дату публикации
        :param html: BeautifulSoup
            html разметка в виде класса BeautifulSoup
        :return: str
            Дата публикации
        """
        try:
            return html.find("time").text
        except Exception as exe:
            print(f"ISource.Warning: Дата публикации не найдена: {exe}")
            return None

    def _get_atricle_type(self, html: BeautifulSoup) -> str:
        """
        Возвращает тип статьи
        :param html: BeautifulSoup
            html разметка в виде класса BeautifulSoup
        :return: str
            Тип статьи
        """
        try:
            return html.find("li", {"c-article-identifiers__item"}).text
        except Exception as exe:
            print(f"ISource.Warning: Тип статьи не найден: {exe}")
            return None

    def _get_journal_url(self, sub_url: str):
        return sub_url.join(self.config['base_list_url'].split('*****'))

    def _enable_download_headless(self, download_dir):
        # Изменяет директорию для хранения скачанных данных
        self.driver.command_executor._commands["send_command"] = \
            ("POST", '/session/$sessionId/chromium/send_command')
        params = {'cmd': 'Page.setDownloadBehavior', 'params':
            {'behavior': 'allow', 'downloadPath': download_dir}}
        self.driver.execute("send_command", params)

    def _dowlnoad_article(self, article_url: str, journal_index: int):
        try:
            self._enable_download_headless(
                f"{self.config['download_paths']}/{self.config['names'][journal_index]}")
            self.driver.get(f'{article_url}.pdf')
        except Exception as exe:
            print(f"ISource.Error: Ошибка загрузки файла статьи: {exe}")
            return None

    def _get_article_list(self, page_url):
        """

        :param page_url:
        :return:
        """
        self.driver.get(page_url)
        # Переводим в HTML
        html = BeautifulSoup(self.driver.page_source, 'lxml')
        ###
        hrefs = html.find_all("a", {'data-track-action': 'view article'})

        hrefs_arr = []
        for href in hrefs:
            hrefs_arr.append(f"{self.config['base_res_url']}{href['href']}")
        return hrefs_arr

    def _do_pager(self, journal_url: str, journal_index: int):
        # Проход по страничкам
        range_pages = self._get_range(self.config['pagers'][journal_index])
        for pager in range_pages:
            print(f"ISource.Info: Страница {pager} из {range_pages[-1]} ({self.config['pagers'][journal_index]}).")
            try:
                # Получение списка статей со странички
                page_url = f'{journal_url}{str(pager)}'
                article_list = self._get_article_list(page_url)
                for article_url in article_list:
                    try:
                        metadata = self._get_metadata(article_url, journal_index)
                        # Загрузка файла
                        self._dowlnoad_article(article_url, journal_index)
                        # Сохранение метадаты
                        self._save_metadata(metadata_info=metadata, journal_index=journal_index)
                        # Задержка
                        time.sleep(self.config['article_delay'])
                    except Exception as exe:
                        print(f"ISource.Error: Ошибка загрузки статьи: {exe}")
                        continue  # Переходим к следующей статье
            except Exception as exe:
                print(f"ISource.Error: Ошибка загрузки списка статей: {exe}")
                continue  # Переход к следущей странице

    def run(self) -> None:
        """
        Запускает на выполнение источник
        :return:  None
        """
        # Создание драйвера
        self._create_driver()
        # Проход по журналам
        for index, sub_url in enumerate(self.config['journal_sub_url']):
            print(f"ISource.Info: Скачивание журанала {self.config['names'][index]}.")
            journal_url = self._get_journal_url(sub_url)
            self._do_pager(journal_url, index)
            time.sleep(self.config['journal_delay'])
