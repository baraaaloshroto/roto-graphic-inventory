using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string inventoryFile = "inventory_items.txt";
string historyFile = "history.txt";

string employeePassword = "emp123";  // كلمة مرور الموظفين (عرض + سحب وإضافة فقط)
string adminPassword = "2687112";    // كلمة المرور الشخصية الجديدة للإدارة

if (!File.Exists(inventoryFile))
{
    File.WriteAllLines(inventoryFile, new[]
    {
        "1. بولي استر:3000",
        "2. بولي ايثلين شفاف:5000",
        "3. بوب (BOPP):2500",
        "4. بولي ايثلين ابيض:3500",
        "5. بوب ميتلايز:2000",
        "6. بولي استر ميتلايز:1500",
        "7. بيرلايز:1800",
        "8. كاست (Cast):2200",
        "9. المنيوم:1000",
        "10. احبار:800",
        "11. مذيبات:600",
        "12. صمغ:400"
    });
}
if (!File.Exists(historyFile))
{
    File.WriteAllText(historyFile, $"--- Roto Graphic Log Started ---{Environment.NewLine}");
}

app.MapGet("/", async context =>
{
    if (!context.Request.Cookies.TryGetValue("role", out string role) || (role != "employee" && role != "admin"))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync($@"
            <!DOCTYPE html>
            <html lang='ar' dir='rtl'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Roto Graphic - تسجيل الدخول</title>
                <style>
                    body {{ font-family: Tahoma, sans-serif; padding: 30px; background: #f4f7f6; color: #333; display: flex; justify-content: center; align-items: center; height: 80vh; }}
                    .login-card {{ background: white; padding: 25px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.1); width: 100%; max-width: 350px; text-align: center; }}
                    h2 {{ color: #0275d8; margin-bottom: 20px; }}
                    input, button {{ width: 100%; padding: 12px; margin: 10px 0; font-size: 16px; border-radius: 6px; border: 1px solid #ccc; box-sizing: border-box; }}
                    button {{ background: #0275d8; color: white; border: none; font-weight: bold; cursor: pointer; }}
                </style>
            </head>
            <body>
                <div class='login-card'>
                    <h2>Roto Graphic</h2>
                    <p>أدخل كلمة المرور للمتابعة</p>
                    <form method='POST' action='/login'>
                        <input type='password' name='password' placeholder='كلمة المرور' required autofocus />
                        <button type='submit'>دخول</button>
                    </form>
                </div>
            </body>
            </html>
        ");
        return;
    }

    var lines = File.ReadAllLines(inventoryFile);
    string historyLogs = File.Exists(historyFile) ? File.ReadAllText(historyFile) : "لا توجد حركات مسجلة.";

    double totalWeight = 0;
    string itemsHtml = "";

    foreach (var line in lines)
    {
        var parts = line.Split(':');
        if (parts.Length == 2)
        {
            string name = parts[0];
            double stock = double.Parse(parts[1]);
            totalWeight += stock;

            itemsHtml += $@"
                <div class='item-box'>
                    <b>الخامة:</b> {name}<br>
                    <b>الرصيد المتبقي:</b> <span style='color:green; font-weight:bold;'>{stock} كجم</span>
                    
                    <form method='POST' action='/dispatch' style='margin-top:8px;'>
                        <input type='hidden' name='itemName' value='{name}' />
                        <input type='number' step='any' name='qty' placeholder='سحب للصالة (كجم)' required />
                        <button type='submit' class='dispatch'>سحب</button>
                    </form>

                    <form method='POST' action='/add' style='margin-top:4px;'>
                        <input type='hidden' name='itemName' value='{name}' />
                        <input type='number' step='any' name='newQty' placeholder='إضافة شحنة (كجم)' required />
                        <button type='submit' class='add'>إضافة</button>
                    </form>
                </div>
                <hr style='border:0; border-top:1px dashed #ccc; margin:12px 0;'>
            ";
        }
    }

    string adminSection = "";
    if (role == "admin")
    {
        adminSection = $@"
            <div style='background: #e2e8f0; padding: 15px; border-radius: 8px; margin-bottom: 20px;'>
                <h3 style='margin-top:0; color:#1d4ed8; font-size:15px;'>لوحة تحكم الإدارة:</h3>
                <form method='POST' action='/addItem'>
                    <input type='text' name='newItemName' placeholder='اسم الخامة الجديدة' required />
                    <input type='number' step='any' name='newItemStock' placeholder='الوزن الابتدائي (كجم)' required />
                    <button type='submit' style='background: #2563eb; color:white; border:none; padding:10px; border-radius:6px; font-weight:bold; cursor:pointer; width:100%;'>إضافة خامة جديدة للنظام</button>
                </form>
            </div>
        ";
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync($@"
        <!DOCTYPE html>
        <html lang='ar' dir='rtl'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Roto Graphic - إدارة المخزن</title>
            <style>
                body {{ font-family: Tahoma, sans-serif; padding: 15px; background: #f4f7f6; color: #333; }}
                .card {{ background: white; padding: 20px; border-radius: 12px; box-shadow: 0 4px 10px rgba(0,0,0,0.1); max-width: 500px; margin: auto; }}
                h2 {{ color: #0275d8; text-align: center; }}
                .top-bar {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px; }}
                .total-box {{ background: #0275d8; color: white; padding: 12px; border-radius: 8px; text-align: center; font-size: 16px; font-weight: bold; margin-bottom: 15px; }}
                .logout-btn {{ background: #d9534f; color: white; border: none; padding: 6px 12px; border-radius: 4px; cursor: pointer; font-size: 13px; text-decoration: none; }}
                .item-box {{ background: #e9ecef; padding: 10px 12px; border-radius: 8px; font-size: 15px; }}
                input, button {{ width: 100%; padding: 8px; margin: 4px 0; font-size: 14px; border-radius: 6px; border: 1px solid #ccc; box-sizing: border-box; }}
                button.dispatch {{ background: #d9534f; color: white; border: none; font-weight: bold; cursor: pointer; }}
                button.add {{ background: #5cb85c; color: white; border: none; font-weight: bold; cursor: pointer; }}
                pre {{ background: #222; color: #00ff00; padding: 10px; border-radius: 6px; font-size: 11px; overflow-x: auto; text-align: left; direction: ltr; }}
            </style>
        </head>
        <body>
            <div class='card'>
                <div class='top-bar'>
                    <h2>Roto Graphic</h2>
                    <a href='/logout' class='logout-btn'>تسجيل خروج</a>
                </div>
                <div class='total-box'>
                    إجمالي وزن الخامات بالمخزن: {totalWeight} كجم
                </div>
                {adminSection}
                {itemsHtml}
                <h3 style='margin-top:20px; font-size: 15px;'>سجل الحركات:</h3>
                <pre>{historyLogs}</pre>
            </div>
        </body>
        </html>
    ");
});

app.MapPost("/login", async context =>
{
    var form = await context.Request.ReadFormAsync();
    string pass = form["password"];

    if (pass == adminPassword)
    {
        context.Response.Cookies.Append("role", "admin", new CookieOptions { Expires = DateTimeOffset.Now.AddDays(7) });
    }
    else if (pass == employeePassword)
    {
        context.Response.Cookies.Append("role", "employee", new CookieOptions { Expires = DateTimeOffset.Now.AddDays(7) });
    }
    context.Response.Redirect("/");
});

app.MapGet("/logout", context =>
{
    context.Response.Cookies.Delete("role");
    context.Response.Redirect("/");
    return Task.CompletedTask;
});

app.MapPost("/addItem", async context =>
{
    if (!context.Request.Cookies.TryGetValue("role", out string role) || role != "admin") { context.Response.Redirect("/"); return; }

    var form = await context.Request.ReadFormAsync();
    string newName = form["newItemName"].ToString().Trim();
    if (double.TryParse(form["newItemStock"], out double newStock) && !string.IsNullOrEmpty(newName))
    {
        var lines = File.ReadAllLines(inventoryFile).ToList();
        lines.Add($"{newName}:{newStock}");
        File.WriteAllLines(inventoryFile, lines);
        File.AppendAllText(historyFile, $"[{DateTime.Now}] Admin Added New Material: {newName} ({newStock}kg){Environment.NewLine}");
    }
    context.Response.Redirect("/");
});

app.MapPost("/dispatch", async context =>
{
    if (!context.Request.Cookies.TryGetValue("role", out string role) || (role != "employee" && role != "admin")) { context.Response.Redirect("/"); return; }

    var form = await context.Request.ReadFormAsync();
    string targetItem = form["itemName"];
    if (double.TryParse(form["qty"], out double qty) && qty > 0)
    {
        var lines = File.ReadAllLines(inventoryFile).ToList();
        for (int i = 0; i < lines.Count; i++)
        {
            var parts = lines[i].Split(':');
            if (parts[0] == targetItem)
            {
                double stock = double.Parse(parts[1]);
                if (stock >= qty)
                {
                    double remaining = stock - qty;
                    lines[i] = $"{targetItem}:{remaining}";
                    File.WriteAllLines(inventoryFile, lines);
                    File.AppendAllText(historyFile, $"[{DateTime.Now}] Dispatched {qty}kg from {targetItem} | Remaining: {remaining}kg{Environment.NewLine}");
                }
                break;
            }
        }
    }
    context.Response.Redirect("/");
});

app.MapPost("/add", async context =>
{
    if (!context.Request.Cookies.TryGetValue("role", out string role) || (role != "employee" && role != "admin")) { context.Response.Redirect("/"); return; }

    var form = await context.Request.ReadFormAsync();
    string targetItem = form["itemName"];
    if (double.TryParse(form["newQty"], out double newQty) && newQty > 0)
    {
        var lines = File.ReadAllLines(inventoryFile).ToList();
        for (int i = 0; i < lines.Count; i++)
        {
            var parts = lines[i].Split(':');
            if (parts[0] == targetItem)
            {
                double stock = double.Parse(parts[1]);
                double total = stock + newQty;
                lines[i] = $"{targetItem}:{total}";
                File.WriteAllLines(inventoryFile, lines);
                File.AppendAllText(historyFile, $"[{DateTime.Now}] Added {newQty}kg to {targetItem} | Total: {total}kg{Environment.NewLine}");
                break;
            }
        }
    }
    context.Response.Redirect("/");
});

app.Run();