using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ExpenseTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        public DashboardController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<ActionResult> Index()
        {
            //Last 7 days
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;

            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            //Total Income
            int TotalIncome = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("C0");

            //Total Expense
            int TotalExpense = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(j => j.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("C0");


            //Balance
            int Balance = TotalIncome - TotalExpense;
            ViewBag.Balance = Balance.ToString("C0");
            //CultureInfo culture =CultureInfo.CreateSpecificCulture("en-US");
            //culture.NumberFormat.CurrencyNegativePattern = 1;
            //ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            //Doughnut Chart -Expense By Category
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(i=>i.Category.Type=="Expense") //篩選類別為"Expense"
                .GroupBy(j=>j.Category.CategoryId)    //根據"CategoryId"紀錄排序
                .Select(k=>new                        
                {
                    categoryTitleWithIcon=k.First().Category.Icon+" "+k.First().Category.Title,     //將類別的Icon和Title組合成一個字串
                    amount = k.Sum(j=>j.Amount),                                                    //計算該類別的總支出金額
                    formattedAmount=k.Sum(j=>j.Amount).ToString("C0"),                              //將總支出金額格式化為貨幣格式的字串(例如，NT$1,000)，這樣顯示起來更清晰
                })
                .OrderByDescending(l=>l.amount)
                .ToList();

            //Spline Chart - Income vs Expense
            //Income
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("MMM-dd"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            //Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("MMM-dd"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();
            //Combine Income & Expense
            string[] last7Days =Enumerable.Range(0,7)
                .Select(i=>StartDate.AddDays(i).ToString("MMM-dd"))
                .ToArray();

            ViewBag.SplineChartData = from day in last7Days
                                      join income in IncomeSummary on day equals income.day
                                      into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };
            //Recent Transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(j => j.Category)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }
    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}
