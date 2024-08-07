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
                .ToList();

            return View();
        }
    }
}
