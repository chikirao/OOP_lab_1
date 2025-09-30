using System;

namespace VendingMachine
{
  // Класс для продуктов
  public class Product
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public int Price { get; set; }
    public int Quantity { get; set; }

    public Product(int id, string name, int price, int quantity)
    {
      this.Id = id;
      this.Name = name;
      this.Price = price;
      this.Quantity = quantity; // this не обязательно, но так принято
    }

    public override string ToString() // Для проверки
    {
      return $"{Id}. {Name} - {Price} ₽; Остаток: {Quantity}";
    }
  }

  // Класс для самой машины
  public class VendingMachine
  {
    // Разрешенные номиналы
    private static readonly int[] AllowedDenominations = [10, 5, 2, 1]; // collection expressions с С# 12, в старых версиях new[] { 10, 5, 2, 1 }

    // Ныняшний набор товаров
    private readonly List<Product> _products = []; // new List<Product>()

    // Сколько монет вставил пользователь
    private readonly Dictionary<int, int> _insertedCoins = []; // new Dictionary<int, int>();

    // Вставленные монеты в рублях
    public int InsertedRubles { get; private set; } = 0;

    // Счетчик выручки
    private int _revenue = 0;

    // Пароль от админки
    private const string AdminPass = "1235";

    // Генератор стартовых товаров для примера
    public VendingMachine()
    {
      _products.Add(new Product(1, "Вода", 5, 8));
      _products.Add(new Product(2, "Сникерс", 20, 6));
      _products.Add(new Product(3, "Баунти", 15, 5));
      _products.Add(new Product(4, "Чипсы", 13, 4));
      _products.Add(new Product(5, "Банан", 7, 7));
    }

    // Форматирование суммы под вид "n ₽"
    public static string FormatRub(int rubles)
    {
      return $"{rubles} ₽";
    }

    // Размен сдачи, жадный алгоритм
    private Dictionary<int, int> MakeChange(int change)
    {
      var result = new Dictionary<int, int>();
      if (change <= 0) return result;

      int remaining = change;

      foreach (var coin in AllowedDenominations)
      {
        if (remaining <= 0) break;

        int take = remaining / coin;
        if (take > 0)
        {
          result[coin] = take;
          remaining -= take * coin;
        }
      }

      return result;
    }

    // Вывод продуктов
    public void ShowProducts()
    {
      Console.WriteLine("\nСписок товаров:");
      foreach (var product in _products)
      {
        Console.WriteLine($"  {product.Id}. {product.Name} - {product.Price}₽. Доступно: {product.Quantity}");
      }
    }

    // Показать разрешенные номиналы
    public void ShowAllowedDenominations()
    {
      Console.WriteLine("Разрешенные номиналы: " + string.Join(", ", AllowedDenominations.Select(FormatRub)));
    }

    // Вставка монет
    public bool InsertCoin(int coin)
    {
      if (!AllowedDenominations.Contains(coin))
      {
        Console.WriteLine("  Такой номинал не принимается!");
        return false;
      }

      InsertedRubles += coin;
      _insertedCoins[coin] = _insertedCoins.GetValueOrDefault(coin) + 1;

      Console.WriteLine($"Принято {FormatRub(coin)}. Текущая сумма монет: {FormatRub(InsertedRubles)}.");
      return true;
    }

    // Отмена операции, возврат средств
    public Dictionary<int, int> CancelXRefund()
    {
      if (InsertedRubles == 0 || _insertedCoins.Count == 0)
      {
        Console.WriteLine("  Вы не вставили монеты, возвращать нечего.");
        return [];
      }

      var refund = new Dictionary<int, int>(_insertedCoins); // Сохраняем копию
      Console.WriteLine($"Отмена операции...\nВозврат средств: {FormatRub(InsertedRubles)}.");
      _insertedCoins.Clear();
      InsertedRubles = 0;

      return refund;
    }

    // Покупка товара
    public bool Purchase(int productId)
    {
      // Отменяем покупку если 0
      if (productId == 0)
      {
        Console.WriteLine("  Покупка отменена!");
        return false;
      }

      // Поиск продукта по ID
      var product = _products.Find(p => p.Id == productId);
      if (product == null)
      {
        Console.WriteLine("Товар не найден.");
        return false;
      }

      // Проверка есть ли в автомате
      if (product.Quantity <= 0)
      {
        Console.WriteLine("Товар закончился.");
        return false;
      }

      // Проверка достаточно ли денег
      int deficit = product.Price - InsertedRubles;
      if (deficit > 0)
      {
        Console.WriteLine($"  Недостаточно средств!\n  Необходимо доплатить: {FormatRub(deficit)}.");
        return false;
      }

      // Подсчет сдачи
      int changeCalc = InsertedRubles - product.Price;
      Dictionary<int, int>? changeToGive = null; // ? = nullable
      if (changeCalc > 0)
      {
        changeToGive = MakeChange(changeCalc);
      }

      if (changeCalc > 0)
      {
        Console.WriteLine($"Сдача: {FormatRub(changeCalc)}.");
        Console.WriteLine("Состав сдачи:");

        // Логика для вывода монет
        if (changeToGive == null)
        {
          Console.WriteLine("  Ошибка!!!\nНе получилось посчитать сдачу. changeToGive = null.");
          return false;
        }
        foreach (var coin in AllowedDenominations)
        {
          if (changeToGive.TryGetValue(coin, out var cnt) && cnt > 0)
          {
            Console.WriteLine($" * {FormatRub(coin)} - {cnt} шт.");
          }
        }
      }

      // Выдаем товар
      product.Quantity--;
      _revenue += product.Price;
      Console.WriteLine($"Выдан товар: {product.Name}.");

      // Очистка и выход
      _insertedCoins.Clear();
      InsertedRubles = 0;

      return true;
    }

    // Секция АДМИН ПАНЕЛИ
    public void AdminPanel()
    {
      Console.Write("Введите ПИНкод: ");
      var pin = Console.ReadLine();
      if (pin != AdminPass)
      {
        Console.WriteLine("  Неверный ПИНкод.");
        return;
      }

      while (true)
      {
        Console.WriteLine("\n  АДМИН-ПАНЕЛЬ");
        Console.WriteLine(" 1. - Показать товары.");
        Console.WriteLine(" 2. - Пополнить остаток товара.");
        Console.WriteLine(" 3. - Создать новый товар.");
        Console.WriteLine(" 4. - Собрать выручку.");
        Console.WriteLine(" 0. - Выйти в режим покупателя.\n");

        Console.Write("ВВОД: ");
        var command = Console.ReadLine();
        Console.WriteLine();

        if (command == "0") break;

        switch (command)
        {
          case "1":
            ShowProducts();
            Console.WriteLine($"Текущая выручка: {FormatRub(_revenue)}");
            break;

          case "2":
            AdminRestockProduct();
            break;

          case "3":
            AdminNewProduct();
            break;

          case "4":
            AdminRevenue();
            break;

          default:
            Console.WriteLine("  Неизвестная комманда!");
            break;
        }
      }
    }

    // Админка: пополнить товары
    private void AdminRestockProduct()
    {
      ShowProducts();
      Console.Write("Введите ID товара для пополнения: ");
      if (!int.TryParse(Console.ReadLine(), out var id))
      {
        Console.WriteLine("  Некорректный ID.");
        return;
      }

      // Поиск товара
      var product = _products.FirstOrDefault(p => p.Id == id);
      if (product == null)
      {
        Console.WriteLine("  Товар не найден.");
        return;
      }

      // Пополняем на конкретное число
      Console.Write("Кол-во пополнения: ");
      if (!int.TryParse(Console.ReadLine(), out var cnt) || cnt <= 0)
      {
        Console.WriteLine("  Некорректное кол-во!");
        return;
      }

      // Увеличиваем кол-во
      product.Quantity += cnt;
      Console.WriteLine("Добавлено.");
      Console.WriteLine($"Остаток товара сейчас \"{product.Name}\" - {product.Quantity} шт.");
    }

    // Создать новый продукт
    private void AdminNewProduct()
    {
      // Ввод названия
      Console.Write("Введите название товара: ");
      var name = (Console.ReadLine() ?? "").Trim(); // На всякий обрезаем пробелы вокруг
      if (string.IsNullOrWhiteSpace(name))
      {
        Console.WriteLine("  Вы ввели пустое название.\nТовар не создан!");
        return;
      }

      // Ввод цены
      Console.Write("Введите цену в рублях: ");
      if (!int.TryParse(Console.ReadLine(), out var price) || price <= 0)
      {
        Console.WriteLine("  Некорректная цена.");
        return;
      }

      // Ввод колличества
      Console.Write("Введине кол-во товара: ");
      if (!int.TryParse(Console.ReadLine(), out var quantity))
      {
        Console.WriteLine("  Колличество введено некорректно!");
      }

      // Назначаем ID.
      int newid;
      if (_products.Count == 0)
      {
        newid = 1;
      }
      else
      {
        int maxid = _products[0].Id;

        for (int i = 1; i < _products.Count; i++)
        {
          if (_products[i].Id > maxid)
          {
            maxid = _products[i].Id;
          }
        }

        newid = maxid + 1;
      }

      _products.Add(new Product(newid, name, price, quantity));
      Console.WriteLine($"Товар добавлен id:{newid} - {name}, {FormatRub(price)}. {quantity} шт.");
    }

    // Собрать выручку
    private void AdminRevenue()
    {
      if (_revenue <= 0)
      {
        Console.WriteLine("  Выручка отсутствует!");
        return;
      }

      Console.WriteLine($"  Выручка к сбору: {FormatRub(_revenue)}");
      Console.Write("Введите сумму для вывода (0 для отмены): ");

      var revenueInput = (Console.ReadLine() ?? "").Trim();

      if (!int.TryParse(revenueInput, out var revenueToCollect) || revenueToCollect < 0)
      {
        Console.WriteLine("  Сумма введена некорректно!");
        return;
      }

      if (revenueToCollect == 0)
      {
        Console.WriteLine("  Вывод отменен.");
        return;
      }

      if (revenueToCollect > _revenue)
      {
        Console.WriteLine("  Введенная сумма больше доступной!");
        return;
      }

      _revenue -= revenueToCollect;
      Console.WriteLine($"Вы забрали: {FormatRub(revenueToCollect)}.\nВ автомате осталось: {FormatRub(_revenue)}.");
    }
  }

  public class Program
  {
    public static void Main()
    {
      Console.OutputEncoding = System.Text.Encoding.UTF8; // Чтоб символ рубля выводился нормально
      var machine = new VendingMachine(); // Создаем сам объект вендингого автомата

      while (true)
      {
        Console.WriteLine("\nВЕНДИНГОВЫЙ АВТОМАТ: ");
        Console.WriteLine("  1. - Показать список товаров.");
        Console.WriteLine("  2. - Вставить монету.");
        Console.WriteLine("  3. - Купить товар.");
        Console.WriteLine("  4. - Отмена, возврат монет.");
        Console.WriteLine("  5. - Войти в админ панель (пароль обязателен).");
        Console.WriteLine("  0. - Выход из сессии.");
        Console.Write("ВВОД: ");
        var command = Console.ReadLine();

        if (command == "0") break;

        switch (command)
        {
          case "1":
            machine.ShowProducts();
            Console.WriteLine($"Текущая внесенная сумма: {VendingMachine.FormatRub(machine.InsertedRubles)}.");
            break;

          case "2":
            machine.ShowAllowedDenominations();
            Console.Write("Введите номинал монеты: ");
            if (!int.TryParse(Console.ReadLine(), out var coin) || coin <= 0)
            {
              Console.WriteLine("  Данный номинал не принимается!");
              break;
            }
            machine.InsertCoin(coin);
            break;

          case "3":
            machine.ShowProducts();
            Console.WriteLine($"\nВнесено (руб): {VendingMachine.FormatRub(machine.InsertedRubles)}.");
            Console.Write("Введите ID товара (0 для отмены): ");
            if (!int.TryParse(Console.ReadLine(), out var productId))
            {
              Console.WriteLine("  Некорректный ID!");
              break;
            }
            machine.Purchase(productId);
            break;

          case "4":
            var refund = machine.CancelXRefund();
            if (refund.Count > 0)
            {
              Console.WriteLine("Возврат монет: ");
              foreach (var coinD in refund.Keys.OrderByDescending(i => i))
              {
                Console.WriteLine($"  {VendingMachine.FormatRub(coinD)} - {refund[coinD]} шт.");
              }
            }
            break;

          case "5":
            machine.AdminPanel();
            break;

          default:
            Console.WriteLine("  Такой команды нет!");
            break;
        }
      }

      // Если цикл прекратился - пользователь закончил сессию
      Console.WriteLine("Выход...\nПока 💋💋💋");
    }
  }
}