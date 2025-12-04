using System.ComponentModel.DataAnnotations;

namespace WorkFlowBasic.Models;

public class BotKnowledge
{
    public int Id { get; set; }

    // Hangi kelimeler geçince bu cevap verilsin? (Virgülle ayrılmış)
    // Örn: "şifre,parola,giriş yapamıyorum,unuttum"
    [Required]
    public required string Keywords { get; set; }

    // Botun vereceği cevap
    [Required]
    public required string Response { get; set; }

    // Cevap içinde link var mı? (Opsiyonel)
    public string? LinkUrl { get; set; }
}