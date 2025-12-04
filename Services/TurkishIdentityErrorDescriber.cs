using Microsoft.AspNetCore.Identity;

namespace WorkFlowBasic.Services;

public class TurkishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError()
        => new IdentityError { Code = nameof(DefaultError), Description = "Bilinmeyen bir hata oluştu." };

    public override IdentityError ConcurrencyFailure()
        => new IdentityError { Code = nameof(ConcurrencyFailure), Description = "Veri başka bir kullanıcı tarafından değiştirilmiş. Lütfen sayfayı yenileyin." };

    public override IdentityError PasswordTooShort(int length)
        => new IdentityError { Code = nameof(PasswordTooShort), Description = $"Şifreniz en az {length} karakter olmalıdır." };

    public override IdentityError PasswordRequiresNonAlphanumeric()
        => new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Şifreniz en az bir sembol içermelidir (!, +, ? vb.)." };

    public override IdentityError PasswordRequiresDigit()
        => new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "Şifreniz en az bir rakam (0-9) içermelidir." };

    public override IdentityError PasswordRequiresLower()
        => new IdentityError { Code = nameof(PasswordRequiresLower), Description = "Şifreniz en az bir küçük harf (a-z) içermelidir." };

    public override IdentityError PasswordRequiresUpper()
        => new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "Şifreniz en az bir büyük harf (A-Z) içermelidir." };

    public override IdentityError DuplicateUserName(string userName)
        => new IdentityError { Code = nameof(DuplicateUserName), Description = $"'{userName}' kullanıcı adı zaten kullanılıyor." };

    public override IdentityError DuplicateEmail(string email)
        => new IdentityError { Code = nameof(DuplicateEmail), Description = $"'{email}' e-posta adresi zaten kayıtlı." };

    public override IdentityError InvalidUserName(string userName)
        => new IdentityError { Code = nameof(InvalidUserName), Description = $"Kullanıcı adı geçersiz karakterler içeriyor." };

    public override IdentityError InvalidEmail(string email)
        => new IdentityError { Code = nameof(InvalidEmail), Description = $"Geçersiz e-posta adresi." };
}