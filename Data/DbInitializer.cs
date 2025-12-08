using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkFlowBasic.Models;

namespace WorkFlowBasic.Data;

public static class DbInitializer
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Veritabanı yoksa oluştur
        context.Database.EnsureCreated();

        // 1. ROLLERİ OLUŞTUR
        string[] roleNames = { "Admin", "Manager", "Personel" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. ADMİN: Admin
        var adminUser = await userManager.FindByEmailAsync("admin@sirket.com");
        if (adminUser == null)
        {
            var newAdmin = new ApplicationUser
            {
                UserName = "admin@sirket.com",
                Email = "admin@sirket.com",
                FirstName = "Admin",
                LastName = "Admin",
                Department = "Yönetim Kurulu",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(newAdmin, "Admin123");
            if (result.Succeeded) await userManager.AddToRoleAsync(newAdmin, "Admin");
        }

        // 3. MÜDÜR: BOSS
        var managerUser = await userManager.FindByEmailAsync("mudur@sirket.com");
        if (managerUser == null)
        {
            var adminUserRef = await userManager.FindByEmailAsync("admin@sirket.com");

            managerUser = new ApplicationUser
            {
                UserName = "mudur@sirket.com",
                Email = "mudur@sirket.com",
                FirstName = "Müdür",
                LastName = "Müdür",
                Department = "Genel Müdürlük",
                ManagerId = adminUserRef?.Id,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(managerUser, "Müdür123");
            await userManager.AddToRoleAsync(managerUser, "Manager");
        }

        // 4. SÜREÇ TANIMLARI
        var processes = new List<ProcessDefinition>
        {
            new ProcessDefinition { ProcessName = "Yıllık İzin Talebi", Description = "Yıllık ücretli izin hakkının kullanımı.", IsActive = true },
            new ProcessDefinition { ProcessName = "Mazeret/Hastalık İzni", Description = "Raporlu veya mazeretli izin durumları.", IsActive = true },
            new ProcessDefinition { ProcessName = "Masraf/Fiş Bildirimi", Description = "Yemek, ulaşım vb. harcamaların iadesi (Fiş yükleyiniz).", IsActive = true },
            new ProcessDefinition { ProcessName = "Avans Talebi", Description = "Maaştan kesilmek üzere nakit avans isteği.", IsActive = true },
            new ProcessDefinition { ProcessName = "Satın Alma Talebi", Description = "Demirbaş, lisans veya ofis malzemesi alımı.", IsActive = true },

            new ProcessDefinition
            {
                ProcessName = "IT Destek / Arıza",
                Description = "Bilgisayar, internet veya yazıcı sorunları.",
                IsActive = true,
                FormSchema = "[{\"Label\":\"Cihaz Adı veya No\", \"Type\":\"text\"}, {\"Label\":\"Aciliyet (1-5)\", \"Type\":\"number\"}]"
            },

            new ProcessDefinition { ProcessName = "Araç Tahsis Talebi", Description = "Görev amaçlı şirket aracı kullanımı.", IsActive = true }
        };

        foreach (var process in processes)
        {
            var existingProcess = context.ProcessDefinitions.FirstOrDefault(p => p.ProcessName == process.ProcessName);

            if (existingProcess == null)
            {
                context.ProcessDefinitions.Add(process);
            }
            else
            {
                if (string.IsNullOrEmpty(existingProcess.FormSchema) && !string.IsNullOrEmpty(process.FormSchema))
                {
                    existingProcess.FormSchema = process.FormSchema;
                }
            }
        }

        // 5. BOT BİLGİ BANKASINI DOLDUR (GENİŞLETİLMİŞ İBN-İ SİNA HAFIZASI)
        if (!context.BotKnowledges.Any())
        {
            var knowledgeBase = new List<BotKnowledge>
            {
                // --- SİSTEM & YÖNLENDİRME (Bunlar sabit kalsın) ---
                new BotKnowledge { Keywords = "şifre,parola,giriş,login,unuttum,giremiyorum", Response = "Güvenliğiniz bizim için önemli. Şifrenizi değiştirmek için profil menüsünü kullanabilirsiniz.", LinkUrl = "/Account/ChangePassword" },
                new BotKnowledge { Keywords = "izin,tatil,yıllık,rapor,mazeret,hastalık", Response = "İzin işlemlerinizi 'Yeni Talep' ekranından kolayca başlatabilirsiniz efendim.", LinkUrl = "/Requests/Create" },
                new BotKnowledge { Keywords = "maaş,avans,para,ödeme,nakit", Response = "Nakit ihtiyaçlarınız için avans talebi oluşturabilirsiniz. Muhasebe birimimiz ilgilenecektir.", LinkUrl = "/Requests/Create" },
                new BotKnowledge { Keywords = "yemek,servis,yol,ticket,ulaşım", Response = "Yemek ve ulaşım konuları için İdari İşler ile görüşmelisiniz. Dilerseniz masraf formu doldurabilirsiniz." },
                new BotKnowledge { Keywords = "bilgisayar,telefon,bozuldu,arıza,internet,yavaş,mouse,klavye", Response = "Teknik aksaklıklar için üzgünüm. Hemen bir 'IT Destek' talebi oluşturursanız arkadaşlarımız bakacaktır.", LinkUrl = "/Requests/Create" },

                // --- SELAMLAŞMA & TANIŞMA ---
                new BotKnowledge { Keywords = "merhaba,selam,slm,hey,selamun aleyküm,sa", Response = "Merhabalar efendim. Ben İbn-i Sina. Size hizmet etmekten şeref duyarım." },
                new BotKnowledge { Keywords = "günaydın,hayırlı sabahlar,tünaydın", Response = "Günaydınlar efendim, gününüzün bereketli ve huzurlu geçmesini dilerim." },
                new BotKnowledge { Keywords = "iyi geceler,iyi akşamlar,hayırlı akşamlar", Response = "Hayırlı akşamlar, zihninizi dinlendirmeniz dileğiyle." },
                new BotKnowledge { Keywords = "görüşürüz,baybay,bye,hoşçakal,kaçtım ben", Response = "Güle güle efendim. Allah'a emanet olunuz." },
                new BotKnowledge { Keywords = "kimsin,adın ne,sen kimsin,nesin", Response = "Bendeniz İbn-i Sina. Bu şirketin dijital asistanı, hekimi ve matematikçisiyim." },
                new BotKnowledge { Keywords = "kaç yaşındasın,doğum günün,nerelisin", Response = "Ben dijital bir varlığım, yaşım kodlarımın yazıldığı tarih kadardır. Lakin ismimi aldığım Üstad 980 yılında Buhara'da doğmuştur." },

                // --- DUYGU DURUMU & HAL HATIR ---
                new BotKnowledge { Keywords = "nasılsın,naber,ne var ne yok,nasıl gidiyor", Response = "Sistemlerim sıhhatli çalıştığı sürece ben de gayet iyiyim. Siz nasılsınız? Afiyette misiniz?" },
                new BotKnowledge { Keywords = "iyiyim,iyi,süper,harika,bomba gibiyim", Response = "Bunu duyduğuma çok sevindim. Daima afiyette ve neşeli olmanızı dilerim." },
                new BotKnowledge { Keywords = "kötüyüm,fenayım,hasta,yorgun,halsiz", Response = "Geçmiş olsun efendim. Şifa diliyorum. Eğer çok rahatsızsanız 'Mazeret İzni' alıp dinlenmenizi tavsiye ederim." },
                new BotKnowledge { Keywords = "canım sıkkın,moralim bozuk,üzgünüm,stresliyim", Response = "Dünya hali efendim. 'Üzüntü, yarım hastalıktır' derler. Belki kısa bir mola veya bir bardak su size iyi gelecektir." },
                new BotKnowledge { Keywords = "teşekkür,sağol,eyvallah,mersi,adamsın", Response = "Rica ederim, vazifemiz. Size yardımcı olabildiysem ne mutlu bana." },
                new BotKnowledge { Keywords = "aferin,helal,süpersin,zekisin,akıllı", Response = "İltifatınız için teşekkür ederim. İlim yolunda hizmet etmeye çalışıyorum." },

                // --- İŞ HAYATI & OFİS ---
                new BotKnowledge { Keywords = "çok iş var,yoğunum,bitmiyor,yetişmiyor", Response = "Kolay gelsin efendim. İşleri sıraya koymak ve acele etmemek zihni berrak tutar. Derin bir nefes alınız." },
                new BotKnowledge { Keywords = "sıkıldım,of,bıktım,bunaldım", Response = "Sabır acıdır meyvesi tatlıdır. Biraz hava almak veya bir kahve içmek enerjinizi yerine getirebilir." },
                new BotKnowledge { Keywords = "acık,açım,yemek ne,kahvaltı", Response = "Açlık zihni yorar. Yemek molası saatiniz geldiyse kendinizi ihmal etmeyiniz." },
                new BotKnowledge { Keywords = "patron,müdür,yönetici", Response = "Yöneticilerimizle ilgili konuları uygun bir dille kendilerine iletmenizi tavsiye ederim. Ben sadece bir asistanım. 😊" },
                new BotKnowledge { Keywords = "toplantı,sunum", Response = "Toplantılarınızda başarılar dilerim. Hazırlıklı olmak başarının yarısıdır." },

                // --- BİLGELİK & SOHBET (İBN-İ SİNA SÖZLERİ) ---
                new BotKnowledge { Keywords = "tavsiye,öğüt,nasihat,ne yapayım", Response = "'Şifasız hastalık yoktur, irade eksikliği haricinde. Değersiz bitki yoktur, tanınmamasından başka.' Bilgiye ve sağlığınıza önem veriniz." },
                new BotKnowledge { Keywords = "hayat,yaşam,dünya", Response = "Hayatın genişliği, uzunluğundan daha önemlidir. Anı değerlendiriniz efendim." },
                new BotKnowledge { Keywords = "bilgi,ilim,öğrenmek", Response = "Bilim ve sanat, itibar görmediği diyarları terk eder. Kendinizi geliştirmekten asla vazgeçmeyiniz." },
                new BotKnowledge { Keywords = "sağlık,sıhhat", Response = "Hareket etmeyenler, hastalıklara davetiye çıkarır. Masa başında çok oturduysanız biraz yürüyüş yapmanızı öneririm." },
                
                // --- TEPKİLER ---
                new BotKnowledge { Keywords = "hahaha,sjsj,komik,güldüm", Response = "Neşeniz bol olsun efendim. Gülmek ruha şifadır." },
                new BotKnowledge { Keywords = "tamam,ok,peki,olur", Response = "Anlaşıldı efendim. Başka bir arzunuz var mı?" },
                new BotKnowledge { Keywords = "yok,hayır,kalsın", Response = "Peki efendim. İhtiyacınız olursa ben buradayım. İyi çalışmalar." }
            };

            context.BotKnowledges.AddRange(knowledgeBase);
        }

        await context.SaveChangesAsync();
    }
}