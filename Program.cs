using CampusLostAndFound.Data;
using CampusLostAndFound.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", p =>
        p.WithOrigins("http://localhost:5174")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IListingService, ListingService>();
builder.Services.AddScoped<IClaimService, ClaimService>();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();
app.UseCors("dev");
app.MapGet("/", () => Results.Ok("CampusLostAndFound API is running"));
app.MapGet("/health", () => Results.Ok("OK"));
//app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Listings.Any())
    {
        var listingId = Guid.NewGuid();
        db.Listings.AddRange(
            new CampusLostAndFound.Models.Listing
            {
                Id = listingId,
                OwnerName = "John Doe",
                Type = "Lost",
                Title = "Lost Keys",
                Description = "Silver keychain with 3 keys",
                Category = "Personal",
                Location = "Library",
                EventDate = DateTime.UtcNow.AddDays(-1),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = Guid.NewGuid(),
                OwnerName = "Jane Smith",
                Type = "Found",
                Title = "Blue Umbrella",
                Description = "Small foldable umbrella",
                Category = "Accessories",
                Location = "Cafeteria",
                EventDate = DateTime.UtcNow,
                Status = "Open"
            }
        );
        db.Claims.Add(new CampusLostAndFound.Models.Claim
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            RequesterName = "Alice",
            Message = "I think those are mine!",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");