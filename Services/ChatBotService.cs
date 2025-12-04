using Microsoft.EntityFrameworkCore;
using WorkFlowBasic.Data;
using WorkFlowBasic.Models;
using System.Text.RegularExpressions;
using System.Globalization;

namespace WorkFlowBasic.Services;

public class ChatBotService
{
    private readonly ApplicationDbContext _context;

    public ChatBotService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetResponse(string userMessage, string userId)
    {
        // O anki dili kontrol et (tr veya en)
        var currentCulture = CultureInfo.CurrentUICulture.Name;
        bool isEnglish = currentCulture.StartsWith("en");

        var msg = userMessage.ToLower(new CultureInfo("tr-TR")).Trim();

        // --- 1. MATEMATİK KONTROLÜ ---
        string? mathResult = TryCalculateMath(msg, isEnglish);
        if (mathResult != null)
        {
            return isEnglish
                ? $"🧮 Calculation result: **{mathResult}**"
                : $"🧮 Hesabıma göre sonuç: **{mathResult}**";
        }

        // --- 2. PERSONA (İBN-İ SİNA / AVICENNA) ---
        if (msg == "merhaba" || msg == "selam" || msg == "slm" || msg == "hello" || msg == "hi")
        {
            return isEnglish
                ? "Greetings. I am Avicenna. How can I assist you today?"
                : "Merhabalar efendim. Ben İbn-i Sina. Size hizmet etmekten şeref duyarım.";
        }

        if (msg.Contains("nasılsın") || msg.Contains("how are you"))
        {
            return isEnglish
                ? "My systems are fully operational. How are you?"
                : "Sistemlerim sıhhatli çalıştığı sürece ben de gayet iyiyim. Siz nasılsınız?";
        }

        // --- 3. DİNAMİK VERİ SORGULARI ---

        // Bekleyen İşler
        if (msg.Contains("bekleyen") || msg.Contains("durum") || msg.Contains("pending") || msg.Contains("status"))
        {
            var count = await _context.WorkflowRequests
                .CountAsync(r => r.RequesterId == userId && r.Status == RequestStatus.Pending);

            if (count == 0)
                return isEnglish ? "You have no pending requests. ✨" : "Şu an bekleyen bir işiniz yok, her şey yolunda. ✨";

            return isEnglish
                ? $"You have **{count}** pending requests."
                : $"Efendim, şu anda onay bekleyen **{count} adet** talebiniz mevcut.";
        }

        // Zimmetler
        if (msg.Contains("zimmet") || msg.Contains("eşya") || msg.Contains("inventory") || msg.Contains("item"))
        {
            var count = await _context.InventoryAssignments
                .CountAsync(x => x.AssignedToUserId == userId && x.ReturnDate == null);

            if (count > 0)
                return isEnglish
                    ? $"You have **{count}** items assigned to you."
                    : $"Üzerinize zimmetli **{count} parça** eşya görünüyor.";
            else
                return isEnglish
                    ? "You have no items assigned."
                    : "Üzerinize kayıtlı eşya bulunmamaktadır.";
        }

        // --- 4. VERİTABANI BİLGİ BANKASI ---
        var knowledgeBase = await _context.BotKnowledges.ToListAsync();
        foreach (var item in knowledgeBase)
        {
            var keywords = item.Keywords.Split(',');
            foreach (var key in keywords)
            {
                if (msg.Contains(key.Trim()))
                {
                    // Veritabanındaki cevap Türkçe ise ve kullanıcı İngilizce konuşuyorsa
                    // Basit bir yönlendirme ekleyebiliriz veya olduğu gibi dönebiliriz.
                    string response = item.Response;

                    if (!string.IsNullOrEmpty(item.LinkUrl))
                    {
                        string btnText = isEnglish ? "Go to Page" : "Sayfaya Git";
                        response += $" <br><a href='{item.LinkUrl}' class='btn btn-sm btn-outline-light mt-2'>{btnText}</a>";
                    }
                    return response;
                }
            }
        }

        // --- 5. ANLAŞILMAYAN DURUM ---
        return isEnglish
            ? "I couldn't quite understand that. 🤔 You can ask about tasks, inventory, or math."
            : "Af buyurun, tam anlayamadım. 🤔 Sohbet edebiliriz, şirketle ilgili sorabilir veya matematik işlemi yaptırabilirsiniz.";
    }

    // --- MATEMATİK MOTORU ---
    private string? TryCalculateMath(string text, bool isEnglish)
    {
        try
        {
            var numbers = Regex.Matches(text, @"-?\d+(\.\d+)?").Select(m => double.Parse(m.Value.Replace(".", ","))).ToList();

            if (text.Contains("faktöriyel") || text.Contains("!") || text.Contains("factorial"))
            {
                if (numbers.Count > 0)
                {
                    int num = (int)numbers[0];
                    long fact = 1;
                    for (int i = 1; i <= num; i++) fact *= i;
                    return $"{num}! = {fact}";
                }
            }

            // Basit 4 işlem kontrolü (Sadece örnek, tüm işlemleri buraya sığdırmadım)
            if (numbers.Count >= 2)
            {
                double n1 = numbers[0];
                double n2 = numbers[1];
                if (text.Contains("+") || text.Contains("add") || text.Contains("topla")) return $"{n1} + {n2} = {n1 + n2}";
                if (text.Contains("-") || text.Contains("minus") || text.Contains("çıkar")) return $"{n1} - {n2} = {n1 - n2}";
                if (text.Contains("*") || text.Contains("multiply") || text.Contains("çarp")) return $"{n1} x {n2} = {n1 * n2}";
                if (text.Contains("/") || text.Contains("divide") || text.Contains("böl")) return $"{n1} / {n2} = {n1 / n2}";
            }

            return null;
        }
        catch
        {
            return isEnglish ? "Math error." : "Hesaplama hatası.";
        }
    }
}