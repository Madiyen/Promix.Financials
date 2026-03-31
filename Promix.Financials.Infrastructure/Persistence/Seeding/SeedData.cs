using Microsoft.EntityFrameworkCore;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Domain.Security;
using Promix.Financials.Domain.Accounting;
namespace Promix.Financials.Infrastructure.Persistence.Seeding;

public static class SeedData
{
    public static async Task EnsureSeedAsync(PromixDbContext db, IPasswordHasher hasher)
    {
        await db.Database.MigrateAsync();
        if (!await db.Currencies.AnyAsync())
        {
            db.Currencies.AddRange(
    new DefaultCurrency("USD", "دولار أمريكي", "US Dollar", "$", 2, true, true, 1),
    new DefaultCurrency("EUR", "يورو", "Euro", "€", 2, true, true, 2),
    new DefaultCurrency("SAR", "ريال سعودي", "Saudi Riyal", "ر.س", 2, true, true, 3),
    new DefaultCurrency("AED", "درهم إماراتي", "UAE Dirham", "د.إ", 2, true, true, 4),
    new DefaultCurrency("IQD", "دينار عراقي", "Iraqi Dinar", "د.ع", 2, true, true, 5),
    new DefaultCurrency("TRY", "ليرة تركية", "Turkish Lira", "₺", 2, true, true, 6),
    new DefaultCurrency("SYP", "ليرة سورية", "Syrian Pound", "£S", 2, true, true, 7)
);

            await db.SaveChangesAsync();
        }
        var hasSyp = await db.Currencies.AnyAsync(x => x.Id == "SYP");
        if (!hasSyp)
        {
            db.Currencies.Add(new DefaultCurrency("SYP", "ليرة سورية", "Syrian Pound", "£S", 2, true, true, 7));
            await db.SaveChangesAsync();
        }
        // 1) Role
        var adminRole = await db.Roles.FirstOrDefaultAsync(x => x.Name == "Admin");
        if (adminRole is null)
        {
            adminRole = new Role("Admin", isSystem: true);
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync();
        }

        // 2) User
        var admin = await db.Users.FirstOrDefaultAsync(x => x.Username == "admin");
        if (admin is null)
        {
            admin = new User("admin", hasher.Hash("Admin@123"));
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }

        // 3) UserRole link
        var hasRoleLink = await db.UserRoles.AnyAsync(x => x.UserId == admin.Id && x.RoleId == adminRole.Id);
        if (!hasRoleLink)
        {
            db.UserRoles.Add(new UserRole(admin.Id, adminRole.Id));
            await db.SaveChangesAsync();
        }

        // طبعاً لا نعتمد على لحظة إنشاء الشركة فقط. نمر على جميع الشركات عند الإقلاع
        // لضمان وجود العملة الأساسية ودليل الحسابات حتى مع قواعد بيانات قديمة.
        var initializer = new CompanyInitializer(db);
        var companyIds = await db.Companies
            .Select(x => x.Id)
            .ToListAsync();

        foreach (var companyId in companyIds)
            await initializer.InitializeAsync(companyId);
    }
}
