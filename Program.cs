using CampusLostAndFound.Data;
using CampusLostAndFound.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", p =>
        p.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:5174",
            "https://campuslostfound-fe.onrender.com")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<EmbeddingService>();
builder.Services.AddScoped<SemanticRerankService>();
builder.Services.AddScoped<N8nWebhookService>();

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

    // Add columns introduced after initial schema creation (EnsureCreated never alters).
    await db.Database.ExecuteSqlRawAsync(@"
        ALTER TABLE listings ADD COLUMN IF NOT EXISTS normalized_location text;
        ALTER TABLE listings ADD COLUMN IF NOT EXISTS ai_summary text;
    ");

    if (!db.Listings.Any())
    {
        var listing1  = Guid.NewGuid();
        var listing2  = Guid.NewGuid();
        var listing3  = Guid.NewGuid();
        var listing4  = Guid.NewGuid();
        var listing5  = Guid.NewGuid();
        var listing6  = Guid.NewGuid();
        var listing7  = Guid.NewGuid();
        var listing8  = Guid.NewGuid();
        var listing9  = Guid.NewGuid();
        var listing10 = Guid.NewGuid();
        var listing11 = Guid.NewGuid();
        var listing12 = Guid.NewGuid();
        var listing13 = Guid.NewGuid();
        var listing14 = Guid.NewGuid();
        var listing15 = Guid.NewGuid();
        var listing16 = Guid.NewGuid();
        var listing17 = Guid.NewGuid();
        var listing18 = Guid.NewGuid();
        var listing19 = Guid.NewGuid();
        var listing20 = Guid.NewGuid();

        db.Listings.AddRange(
            // --- Lost items ---
            new CampusLostAndFound.Models.Listing
            {
                Id = listing1,
                OwnerName = "John Doe",
                Type = "Lost",
                Title = "Silver House Keys",
                Description = "Silver keychain with 3 keys and a small red rubber tag. Lost near the main entrance.",
                Category = "Keys",
                Location = "Library",
                EventDate = DateTime.UtcNow.AddDays(-1),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing2,
                OwnerName = "Maria Gonzalez",
                Type = "Lost",
                Title = "Black MacBook Pro 14\"",
                Description = "Space Black M3 MacBook Pro with a sticker of a mountain on the lid. Charger also missing.",
                Category = "Electronics",
                Location = "Engineering Building – Room 204",
                EventDate = DateTime.UtcNow.AddDays(-2),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing3,
                OwnerName = "Tyler Brooks",
                Type = "Lost",
                Title = "Student ID Card",
                Description = "Campus ID for Tyler Brooks, student number 2021-04872. Please return to security desk.",
                Category = "ID / Cards",
                Location = "Sports Complex",
                EventDate = DateTime.UtcNow.AddDays(-3),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing4,
                OwnerName = "Priya Nair",
                Type = "Lost",
                Title = "Purple Water Bottle",
                Description = "Hydro Flask 32 oz in matte purple with a dent near the bottom. Name written inside cap.",
                Category = "Personal",
                Location = "Gym",
                EventDate = DateTime.UtcNow.AddDays(-4),
                Status = "Claimed"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing5,
                OwnerName = "Sam Okafor",
                Type = "Lost",
                Title = "Calculus Textbook",
                Description = "Stewart Calculus 8th edition, lots of sticky notes inside, name written on first page.",
                Category = "Books",
                Location = "Math Department Hallway",
                EventDate = DateTime.UtcNow.AddDays(-5),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing6,
                OwnerName = "Lena Müller",
                Type = "Lost",
                Title = "Gold Hoop Earrings",
                Description = "Small 18k gold hoop earrings, pair of two. Sentimental value — grandmother's gift.",
                Category = "Jewelry",
                Location = "Cafeteria",
                EventDate = DateTime.UtcNow.AddDays(-6),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing7,
                OwnerName = "James Kim",
                Type = "Lost",
                Title = "Sony WH-1000XM5 Headphones",
                Description = "Black over-ear noise-cancelling headphones in a grey soft case. Left in the quiet study room.",
                Category = "Electronics",
                Location = "Library – 3rd Floor",
                EventDate = DateTime.UtcNow.AddDays(-7),
                Status = "Closed"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing8,
                OwnerName = "Fatima Al-Hassan",
                Type = "Lost",
                Title = "Blue Denim Jacket",
                Description = "H&M medium-size blue denim jacket with a small patch on the right sleeve.",
                Category = "Clothing",
                Location = "Auditorium",
                EventDate = DateTime.UtcNow.AddDays(-8),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing9,
                OwnerName = "Chris Adeyemi",
                Type = "Lost",
                Title = "AirPods Pro (2nd Gen)",
                Description = "White AirPods Pro in white MagSafe case. Engraved initials 'C.A.' on the case lid.",
                Category = "Electronics",
                Location = "Student Union",
                EventDate = DateTime.UtcNow.AddDays(-2),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing10,
                OwnerName = "Nina Petrov",
                Type = "Lost",
                Title = "Green Backpack",
                Description = "Olive green Herschel backpack, medium size. Contains notebooks and a pencil case inside.",
                Category = "Bags",
                Location = "Bus Stop – North Gate",
                EventDate = DateTime.UtcNow.AddDays(-10),
                Status = "Open"
            },
            // --- Found items ---
            new CampusLostAndFound.Models.Listing
            {
                Id = listing11,
                OwnerName = "Jane Smith",
                Type = "Found",
                Title = "Blue Foldable Umbrella",
                Description = "Small navy-blue foldable umbrella, no brand markings. Found under a cafeteria chair.",
                Category = "Accessories",
                Location = "Cafeteria",
                EventDate = DateTime.UtcNow,
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing12,
                OwnerName = "Oliver Tan",
                Type = "Found",
                Title = "Set of Car Keys",
                Description = "Toyota key fob with 2 house keys attached. Found in the parking lot near Block C.",
                Category = "Keys",
                Location = "Parking Lot C",
                EventDate = DateTime.UtcNow.AddDays(-1),
                Status = "Claimed"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing13,
                OwnerName = "Amara Diallo",
                Type = "Found",
                Title = "Prescription Glasses",
                Description = "Thin black metal-frame glasses in a brown leather case. Found on a bench outside the science building.",
                Category = "Accessories",
                Location = "Science Building – Outdoor Bench",
                EventDate = DateTime.UtcNow.AddDays(-2),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing14,
                OwnerName = "Reza Ahmadi",
                Type = "Found",
                Title = "Samsung Galaxy S24",
                Description = "Black Samsung Galaxy S24 with a cracked screen protector and a blue silicone case. Locked.",
                Category = "Electronics",
                Location = "Student Lounge",
                EventDate = DateTime.UtcNow.AddDays(-3),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing15,
                OwnerName = "Sofia Reyes",
                Type = "Found",
                Title = "Pink Water Bottle",
                Description = "Stanley 40 oz tumbler in light pink. Has a 'Be Kind' sticker on the side. Found in lecture hall B.",
                Category = "Personal",
                Location = "Lecture Hall B",
                EventDate = DateTime.UtcNow.AddDays(-1),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing16,
                OwnerName = "Daniel Osei",
                Type = "Found",
                Title = "Wireless Mouse",
                Description = "Logitech MX Master 3 in graphite. Found on a desk in the computer lab.",
                Category = "Electronics",
                Location = "Computer Lab – Room 101",
                EventDate = DateTime.UtcNow.AddDays(-4),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing17,
                OwnerName = "Yuki Tanaka",
                Type = "Found",
                Title = "Gray Wool Scarf",
                Description = "Long gray knitted wool scarf, no brand label. Found hanging on a coat rack in the arts building.",
                Category = "Clothing",
                Location = "Arts Building – Coat Rack",
                EventDate = DateTime.UtcNow.AddDays(-5),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing18,
                OwnerName = "Kwame Asante",
                Type = "Found",
                Title = "USB-C Charging Cable + Adapter",
                Description = "White 2m USB-C cable with a 65W GaN adapter (Anker brand). Found in a study pod.",
                Category = "Electronics",
                Location = "Library – Study Pods",
                EventDate = DateTime.UtcNow.AddDays(-6),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing19,
                OwnerName = "Isabelle Moreau",
                Type = "Found",
                Title = "Student Planner / Notebook",
                Description = "A5 blue hardcover planner with 'Academic Year 2025' on the cover. Has a name on the inside — partially legible.",
                Category = "Books",
                Location = "Administration Building",
                EventDate = DateTime.UtcNow.AddDays(-7),
                Status = "Open"
            },
            new CampusLostAndFound.Models.Listing
            {
                Id = listing20,
                OwnerName = "Marcus Webb",
                Type = "Found",
                Title = "Credit/Debit Card",
                Description = "A Visa debit card found near the vending machines. Handed to security — please contact the security office to retrieve.",
                Category = "ID / Cards",
                Location = "Vending Machine Area",
                EventDate = DateTime.UtcNow.AddDays(-1),
                Status = "Closed"
            }
        );

        db.Claims.AddRange(
            // Claim on listing1 (Lost Silver Keys) — Pending
            new CampusLostAndFound.Models.Claim
            {
                Id = Guid.NewGuid(),
                ListingId = listing1,
                RequesterName = "Alice Chen",
                Message = "I think those are mine! I have a red rubber tag on my keychain too.",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            },
            // Claim on listing2 (Lost MacBook) — Pending
            new CampusLostAndFound.Models.Claim
            {
                Id = Guid.NewGuid(),
                ListingId = listing2,
                RequesterName = "Maria Gonzalez",
                Message = "That's definitely my laptop. I can provide the serial number to confirm.",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow.AddHours(-12)
            },
            // Claim on listing4 (Lost Purple Bottle) — Accepted
            new CampusLostAndFound.Models.Claim
            {
                Id = Guid.NewGuid(),
                ListingId = listing4,
                RequesterName = "Priya Nair",
                Message = "This is my bottle! My name is written inside the cap.",
                Status = "Accepted",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                DecidedAt = DateTime.UtcNow.AddDays(-1)
            },
            // Claim on listing7 (Lost Headphones) — Accepted (listing is Closed)
            new CampusLostAndFound.Models.Claim
            {
                Id = Guid.NewGuid(),
                ListingId = listing7,
                RequesterName = "James Kim",
                Message = "Those are mine. I can show my receipt for the XM5s.",
                Status = "Accepted",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                DecidedAt = DateTime.UtcNow.AddDays(-4)
            },
            // Claim on listing12 (Found Car Keys) — Accepted
            new CampusLostAndFound.Models.Claim
            {
                Id = Guid.NewGuid(),
                ListingId = listing12,
                RequesterName = "Leo Fernandez",
                Message = "Those are my Toyota keys! I was parked in lot C yesterday.",
                Status = "Accepted",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                DecidedAt = DateTime.UtcNow.AddHours(-10)
            },
            // Rejected claim on listing12 — someone else also tried
            new CampusLostAndFound.Models.Claim
            {
                Id = Guid.NewGuid(),
                ListingId = listing12,
                RequesterName = "David Lim",
                Message = "I lost Toyota keys too, could be mine?",
                Status = "Rejected",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                DecidedAt = DateTime.UtcNow.AddHours(-9)
            },
            // Claim on listing14 (Found Samsung) — Pending
            new CampusLostAndFound.Models.Claim
            {
                Id = Guid.NewGuid(),
                ListingId = listing14,
                RequesterName = "Hana Yilmaz",
                Message = "My Galaxy S24 went missing yesterday from the lounge. Blue case sounds right!",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            // Claim on listing9 (Lost AirPods) — Pending
            new CampusLostAndFound.Models.Claim
            {
                Id = Guid.NewGuid(),
                ListingId = listing9,
                RequesterName = "Chris Adeyemi",
                Message = "The initials C.A. match mine — please contact me at chrisa@campus.edu.",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        );

        db.SaveChanges();
    }

    // Runs every startup — only indexes listings where EmbeddingJson is still null, so it's safe to call always
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    await mediator.Send(new CampusLostAndFound.Commands.ReindexListingsCommand());
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");