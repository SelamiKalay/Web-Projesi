using ExcelDataReader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkFlowBasic.Data;
using WorkFlowBasic.Models;
using WorkFlowBasic.Services;

namespace WorkFlowBasic.Controllers;

[Authorize]
public class InventoryController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notificationService;

    public InventoryController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        NotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    // ==========================================
    // YÖNETİCİ İŞLEMLERİ
    // ==========================================

    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Index()
    {
        var items = await _context.InventoryItems.ToListAsync();
        ViewBag.ReturnRequests = await _context.InventoryAssignments
            .Include(x => x.InventoryItem)
            .Include(x => x.AssignedToUser)
            .Where(x => x.IsReturnRequested && x.ReturnDate == null)
            .ToListAsync();

        return View(items);
    }

    [Authorize(Roles = "Manager,Admin")]
    public IActionResult Create() => View();

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(InventoryItem model)
    {
        if (ModelState.IsValid)
        {
            _context.InventoryItems.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Eşya envantere eklendi.";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item != null)
        {
            if (!string.IsNullOrEmpty(item.CurrentOwnerId))
            {
                TempData["Error"] = "Bu eşya şu an bir personelde zimmetli. Önce iade almalısınız.";
                return RedirectToAction(nameof(Index));
            }

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Eşya envanterden silindi.";
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Assign(int id)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item == null) return NotFound();
        var personnel = await _userManager.GetUsersInRoleAsync("Personel");
        ViewBag.UserList = new SelectList(personnel, "Id", "FullName");
        return View(item);
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<IActionResult> Assign(int itemId, string userId)
    {
        var item = await _context.InventoryItems.FindAsync(itemId);
        if (item == null) return NotFound();

        item.CurrentOwnerId = userId;

        var assignment = new InventoryAssignment
        {
            InventoryItemId = itemId,
            AssignedToUserId = userId,
            AssignedDate = DateTime.UtcNow.AddHours(3),
            IsAccepted = false
        };

        _context.InventoryAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(
            userId,
            "Yeni Zimmet 📦",
            $"{item.ItemName} üzerinize zimmetlendi. Lütfen onaylayınız.",
            "/Inventory/MyInventory"
        );

        TempData["Success"] = "Zimmetleme yapıldı ve personele bildirim gönderildi.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> History(int id)
    {
        var history = await _context.InventoryAssignments
            .Include(x => x.AssignedToUser)
            .Include(x => x.InventoryItem)
            .Where(x => x.InventoryItemId == id)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync();

        if (history.Any())
        {
            ViewBag.ItemName = history.First().InventoryItem?.ItemName;
            ViewBag.Serial = history.First().InventoryItem?.SerialNumber;
        }
        else
        {
            var item = await _context.InventoryItems.FindAsync(id);
            ViewBag.ItemName = item?.ItemName;
            ViewBag.Serial = item?.SerialNumber;
        }

        return View(history);
    }

    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> ApproveReturn(int assignmentId)
    {
        var assignment = await _context.InventoryAssignments
            .Include(x => x.InventoryItem)
            .Include(x => x.AssignedToUser) // Bildirim için kullanıcıyı da çekelim
            .FirstOrDefaultAsync(x => x.Id == assignmentId);

        if (assignment != null)
        {
            assignment.ReturnDate = DateTime.UtcNow.AddHours(3);
            if (assignment.InventoryItem != null)
            {
                assignment.InventoryItem.CurrentOwnerId = null;
            }

            await _context.SaveChangesAsync();

            // Personele bildirim gönder: İaden onaylandı
            if (!string.IsNullOrEmpty(assignment.AssignedToUserId))
            {
                await _notificationService.SendNotificationAsync(
                    assignment.AssignedToUserId,
                    "İade Onaylandı ✅",
                    $"{assignment.InventoryItem?.ItemName} iade işleminiz yönetici tarafından onaylandı.",
                    "/Inventory/MyInventory"
                );
            }

            TempData["Success"] = "İade işlemi onaylandı. Eşya depoya döndü.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // PERSONEL İŞLEMLERİ
    // ==========================================

    public async Task<IActionResult> MyInventory()
    {
        var user = await _userManager.GetUserAsync(User);

        var myItems = await _context.InventoryAssignments
            .Include(x => x.InventoryItem)
            .Where(x => x.AssignedToUserId == user.Id && x.ReturnDate == null)
            .ToListAsync();

        return View(myItems);
    }

    // İMZALI KABUL ETME (MÜDÜRE BİLDİRİM GİDER)
    [HttpPost]
    public async Task<IActionResult> AcceptItem(int assignmentId, string signatureData)
    {
        var user = await _userManager.GetUserAsync(User);

        // İlişkili verilerle birlikte çekiyoruz (Item adını bildirimde kullanmak için)
        var assignment = await _context.InventoryAssignments
            .Include(x => x.InventoryItem)
            .FirstOrDefaultAsync(x => x.Id == assignmentId);

        if (assignment != null && assignment.AssignedToUserId == user.Id)
        {
            if (!string.IsNullOrEmpty(signatureData))
            {
                var base64Data = signatureData.Split(',')[1];
                var bytes = Convert.FromBase64String(base64Data);
                var fileName = $"Sig_{assignmentId}_{Guid.NewGuid()}.png";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "signatures");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                await System.IO.File.WriteAllBytesAsync(Path.Combine(path, fileName), bytes);
                assignment.SignaturePath = "/signatures/" + fileName;
            }

            assignment.IsAccepted = true;
            assignment.AcceptanceDate = DateTime.UtcNow.AddHours(3);
            await _context.SaveChangesAsync();

            // --- MÜDÜRE BİLDİRİM GÖNDER ---
            // Eğer personelin yöneticisi varsa ona, yoksa genel bir mekanizma kullanılabilir.
            // Burada kullanıcının ManagerId'sine gönderiyoruz.
            if (user.ManagerId != null)
            {
                await _notificationService.SendNotificationAsync(
                    user.ManagerId,
                    "Zimmet Onaylandı ✍️",
                    $"{user.FullName}, '{assignment.InventoryItem?.ItemName}' eşyasını teslim aldı ve imzaladı.",
                    $"/Inventory/History/{assignment.InventoryItemId}"
                );
            }
            // ------------------------------

            TempData["Success"] = "Zimmet imzalanarak teslim alındı.";
        }
        else
        {
            TempData["Error"] = "Hata oluştu.";
        }
        return RedirectToAction(nameof(MyInventory));
    }

    // İADE TALEBİ (MÜDÜRE BİLDİRİM GİDER)
    [HttpPost]
    public async Task<IActionResult> RequestReturn(int assignmentId)
    {
        var user = await _userManager.GetUserAsync(User);

        var assignment = await _context.InventoryAssignments
            .Include(x => x.InventoryItem)
            .FirstOrDefaultAsync(x => x.Id == assignmentId);

        if (assignment != null && assignment.AssignedToUserId == user.Id)
        {
            assignment.IsReturnRequested = true;
            await _context.SaveChangesAsync();

            // --- MÜDÜRE BİLDİRİM GÖNDER ---
            if (user.ManagerId != null)
            {
                await _notificationService.SendNotificationAsync(
                    user.ManagerId,
                    "İade Talebi ↩️",
                    $"{user.FullName}, '{assignment.InventoryItem?.ItemName}' için iade talebi oluşturdu.",
                    "/Inventory/Index"
                );
            }
            // ------------------------------

            TempData["Success"] = "İade talebiniz yöneticiye iletildi.";
        }
        return RedirectToAction(nameof(MyInventory));
    }
    // ... Diğer metodların altı ...

    // 8. EXCEL İLE TOPLU EŞYA YÜKLEME (IMPORT)
    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<IActionResult> ImportExcel(IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["Error"] = "Lütfen bir Excel dosyası seçiniz.";
            return RedirectToAction(nameof(Index));
        }

        // Excel okuma için gerekli encoding ayarı (Türkçe karakterler için)
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        try
        {
            using (var stream = excelFile.OpenReadStream())
            {
                using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                {
                    // Excel'i DataSet'e çevir
                    var result = reader.AsDataSet();
                    var dataTable = result.Tables[0]; // İlk sayfayı al

                    int addedCount = 0;

                    // Satırları dön (i=1 dedik çünkü ilk satır genelde başlıktır)
                    for (int i = 1; i < dataTable.Rows.Count; i++)
                    {
                        var row = dataTable.Rows[i];

                        // Excel Kolon Sırası: 
                        // 0: Kategori | 1: Ürün Adı | 2: Seri No
                        var category = row[0]?.ToString();
                        var itemName = row[1]?.ToString();
                        var serialNo = row[2]?.ToString();

                        if (!string.IsNullOrEmpty(itemName))
                        {
                            var newItem = new InventoryItem
                            {
                                Category = category ?? "Diğer",
                                ItemName = itemName,
                                SerialNumber = serialNo ?? "Yok",
                                PurchaseDate = DateTime.UtcNow.AddHours(3)
                            };
                            _context.InventoryItems.Add(newItem);
                            addedCount++;
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"{addedCount} adet eşya başarıyla yüklendi.";
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}