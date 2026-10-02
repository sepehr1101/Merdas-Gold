using System.ComponentModel.DataAnnotations;
using System.Data;
using MerdasGold.Features.Customers.Entities;
using MerdasGold.Features.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Customers.Services;

public sealed class CustomerAddressService(IDbContextFactory<MerdasGoldDbContext> factory, CustomerSession session)
{
    public async Task<string> AccountMobileAsync()
    {
        var customer = await session.RequireCustomerAsync();
        await using var db = await factory.CreateDbContextAsync();
        return await db.Users.Where(x => x.Id == customer).Select(x => x.PhoneNumber).SingleAsync() ?? "";
    }
    public async Task<List<CustomerAddress>> ListAsync()
    {
        var id = await session.RequireCustomerAsync();
        await using var db = await factory.CreateDbContextAsync();
        return await db.Set<CustomerAddress>().AsNoTracking().Where(x => x.CustomerId == id)
            .OrderByDescending(x => x.IsDefault).ThenBy(x => x.Id).ToListAsync();
    }
    public async Task SaveAsync(CustomerAddress input)
    {
        var customer = await session.RequireCustomerAsync();
        await using var db = await factory.CreateDbContextAsync();
        var accountMobile = await db.Users.Where(x => x.Id == customer && x.PhoneNumberConfirmed)
            .Select(x => x.PhoneNumber).SingleOrDefaultAsync();
        input.Mobile = CustomerOtpService.NormalizeMobile(accountMobile ?? "");
        input.PostalCode = CustomerOtpService.NormalizeDigits(input.PostalCode);
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var all = await db.Set<CustomerAddress>().Where(x => x.CustomerId == customer).ToListAsync();
        if (input.Id == 0 && all.Count >= 20) throw new ArgumentException("حداکثر ۲۰ آدرس می‌توانید ذخیره کنید.");
        var entity = input.Id == 0 ? new CustomerAddress { CustomerId = customer } : all.SingleOrDefault(x => x.Id == input.Id)
            ?? throw new ArgumentException("آدرس پیدا نشد.");
        if (input.Id == 0) db.Add(entity);
        else db.Entry(entity).Property(x => x.RowVersion).OriginalValue = input.RowVersion;
        entity.Title = input.Title.Trim(); entity.Recipient = input.Recipient.Trim(); entity.Mobile = input.Mobile;
        entity.Province = input.Province.Trim(); entity.City = input.City.Trim(); entity.Street = input.Street.Trim(); entity.PostalCode = input.PostalCode;
        var makeDefault = input.IsDefault || all.Count == 0 || entity.IsDefault;
        if (makeDefault)
        {
            foreach (var item in all) item.IsDefault = false;
            await db.SaveChangesAsync();
        }
        entity.IsDefault = makeDefault;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        input.Id = entity.Id;
        input.RowVersion = entity.RowVersion;
    }
    public async Task DeleteAsync(int id)
    {
        var customer = await session.RequireCustomerAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entity = await db.Set<CustomerAddress>().SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == customer)
            ?? throw new ArgumentException("آدرس پیدا نشد.");
        db.Remove(entity);
        await db.SaveChangesAsync();
        if (entity.IsDefault)
        {
            var next = await db.Set<CustomerAddress>().Where(x => x.CustomerId == customer).OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (next is not null) next.IsDefault = true;
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
    }
}
