# hamster
Дипломная работа "Веб-приложение для учета и контроля инвестиционных портфелей"
Писал все сам - БД, бек, фронт
Технологии:
- ASP.NET MVC 5, C#, SQL (MS SQL Server)
- EF, Microsoft Identity, LINQ
- Google Developers, json
- HTML, CSS, JS, jQuery AJAX

Работа приложения:
Считывает котировки финансовых активов ->  позволяет вести свои портфели с различным набором акций, смотреть динамику, результаты и т.д.

1. Котировки берутся с Google Finance (google sheets) с помощью скрипта обновляются 1 раз в минуту.
2. С помощью форм на веб-интерфейсе можно создавать сделки (покупка, продажа) -> добавление операции и акции в БД
3. По добавленным активам можно смотреть динамику изменения цен в ЛК
4. Можно смотреть все котировки на странице каталог

!!! Код писался в прошлом году. Теперь сам вижу недостатки и слабые места
