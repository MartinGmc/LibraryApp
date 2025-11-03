using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LibraryApp.Domain.Entities;

namespace LibraryApp.Infrastructure.Persistence;

public class BookSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookSeeder> _logger;

    public BookSeeder(AppDbContext context, ILogger<BookSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calculates the ISBN-13 check digit based on the first 12 digits.
    /// ISBN-13 uses EAN-13 algorithm: Multiply each digit by 1 or 3 alternately, sum them, find remainder mod 10, check digit = 10 - remainder.
    /// </summary>
    private static string CalculateIsbn13CheckDigit(string isbn12)
    {
        if (isbn12.Length != 12)
            throw new ArgumentException("ISBN-12 must be exactly 12 digits");

        var sum = 0;
        for (int i = 0; i < 12; i++)
        {
            // Positions start at 1 for EAN-13: position 1 = weight 1, position 2 = weight 3, etc.
            var weight = (i % 2 == 0) ? 1 : 3;
            sum += int.Parse(isbn12[i].ToString()) * weight;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit.ToString();
    }

    /// <summary>
    /// Generates a valid ISBN-13 with proper check digit calculation.
    /// Format: 978-XX-YYYY-ZZZZZ-C where C is the calculated check digit.
    /// </summary>
    private static string GenerateValidIsbn13(string prefix, string middleDigits)
    {
        // Remove hyphens for calculation
        var isbn12 = $"{prefix}{middleDigits}";
        isbn12 = isbn12.Replace("-", "");
        
        if (isbn12.Length != 12)
            throw new ArgumentException($"Combined prefix and middle digits must be 12 characters, got: {isbn12.Length}");

        var checkDigit = CalculateIsbn13CheckDigit(isbn12);
        
        // Format ISBN-13 with hyphens: 978-XX-XXXXXXX-X
        var fullIsbn = isbn12 + checkDigit;
        if (fullIsbn.Length == 13)
        {
            return $"{fullIsbn.Substring(0, 3)}-{fullIsbn.Substring(3, 2)}-{fullIsbn.Substring(5, 7)}-{fullIsbn.Substring(12, 1)}";
        }
        
        // Fallback: return without formatting if length is unexpected
        return fullIsbn;
    }

    public async Task SeedBooksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if books already exist
            if (await _context.Books.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Books already exist in database. Skipping seed.");
                return;
            }

            var books = new List<Book>
            {
                // Sci-Fi Books (Slovak editions)
                new Book { Id = Guid.Parse("a1b2c3d4-e5f6-4789-a012-b3c4d5e6f7a8").ToString(), Name = "2001: Vesmírna Odysea", Author = "Arthur C. Clarke", IssueYear = 2010, ISBN = GenerateValidIsbn13("978", "802490001"), NumberOfPieces = 15 },
                new Book { Id = Guid.Parse("b2c3d4e5-f6a7-4890-b123-c4d5e6f7a8b9").ToString(), Name = "Vládca Kúzliel", Author = "Isaac Asimov", IssueYear = 2012, ISBN = GenerateValidIsbn13("978", "802490002"), NumberOfPieces = 12 },
                new Book { Id = Guid.Parse("c3d4e5f6-a7b8-4901-c234-d5e6f7a8b9c0").ToString(), Name = "Fundamenta Galaktiky", Author = "Isaac Asimov", IssueYear = 2011, ISBN = GenerateValidIsbn13("978", "802490003"), NumberOfPieces = 10 },
                new Book { Id = Guid.Parse("d4e5f6a7-b8c9-4012-d345-e6f7a8b9c0d1").ToString(), Name = "Duna", Author = "Frank Herbert", IssueYear = 2015, ISBN = GenerateValidIsbn13("978", "802490004"), NumberOfPieces = 18 },
                new Book { Id = Guid.Parse("e5f6a7b8-c9d0-4123-e456-f7a8b9c0d1e2").ToString(), Name = "Neuromancer", Author = "William Gibson", IssueYear = 2013, ISBN = GenerateValidIsbn13("978", "802490005"), NumberOfPieces = 14 },
                new Book { Id = Guid.Parse("f6a7b8c9-d0e1-4234-f567-a8b9c0d1e2f3").ToString(), Name = "Konečná Stanica", Author = "Isaac Asimov", IssueYear = 2014, ISBN = GenerateValidIsbn13("978", "802490006"), NumberOfPieces = 16 },
                new Book { Id = Guid.Parse("a7b8c9d0-e1f2-4345-a678-b9c0d1e2f3a4").ToString(), Name = "Hyperión", Author = "Dan Simmons", IssueYear = 2016, ISBN = GenerateValidIsbn13("978", "802490007"), NumberOfPieces = 11 },
                new Book { Id = Guid.Parse("b8c9d0e1-f2a3-4456-b789-c0d1e2f3a4b5").ToString(), Name = "Kvantová Oáza", Author = "Greg Egan", IssueYear = 2012, ISBN = GenerateValidIsbn13("978", "802490008"), NumberOfPieces = 9 },
                new Book { Id = Guid.Parse("c9d0e1f2-a3b4-4567-c890-d1e2f3a4b5c6").ToString(), Name = "Stráž Života", Author = "James Cameron", IssueYear = 2017, ISBN = GenerateValidIsbn13("978", "802490009"), NumberOfPieces = 13 },
                new Book { Id = Guid.Parse("d0e1f2a3-b4c5-4678-d901-e2f3a4b5c6d7").ToString(), Name = "Eden", Author = "Stanisław Lem", IssueYear = 2011, ISBN = GenerateValidIsbn13("978", "802490010"), NumberOfPieces = 17 },
                new Book { Id = Guid.Parse("e1f2a3b4-c5d6-4789-e012-f3a4b5c6d7e8").ToString(), Name = "Solaris", Author = "Stanisław Lem", IssueYear = 2010, ISBN = GenerateValidIsbn13("978", "802490011"), NumberOfPieces = 20 },
                new Book { Id = Guid.Parse("f2a3b4c5-d6e7-4890-f123-a4b5c6d7e8f9").ToString(), Name = "Robot", Author = "Isaac Asimov", IssueYear = 2014, ISBN = GenerateValidIsbn13("978", "802490012"), NumberOfPieces = 15 },
                new Book { Id = Guid.Parse("a3b4c5d6-e7f8-4901-a234-b5c6d7e8f9a0").ToString(), Name = "Válka S Měsícem", Author = "Stanisław Lem", IssueYear = 2013, ISBN = GenerateValidIsbn13("978", "802490013"), NumberOfPieces = 12 },
                new Book { Id = Guid.Parse("b4c5d6e7-f8a9-4012-b345-c6d7e8f9a0b1").ToString(), Name = "Bezduchá Planeta", Author = "Stanisław Lem", IssueYear = 2012, ISBN = GenerateValidIsbn13("978", "802490014"), NumberOfPieces = 14 },
                new Book { Id = Guid.Parse("c5d6e7f8-a9b0-4123-c456-d7e8f9a0b1c2").ToString(), Name = "Černá Díra", Author = "Joe Haldeman", IssueYear = 2015, ISBN = GenerateValidIsbn13("978", "802490015"), NumberOfPieces = 16 },

                // Fantasy Books (Slovak editions)
                new Book { Id = Guid.Parse("d6e7f8a9-b0c1-4234-d567-e8f9a0b1c2d3").ToString(), Name = "Hobit", Author = "J.R.R. Tolkien", IssueYear = 2009, ISBN = GenerateValidIsbn13("978", "802490016"), NumberOfPieces = 25 },
                new Book { Id = Guid.Parse("e7f8a9b0-c1d2-4345-e678-f9a0b1c2d3e4").ToString(), Name = "Spoločenstvo Prsteňa", Author = "J.R.R. Tolkien", IssueYear = 2009, ISBN = GenerateValidIsbn13("978", "802490017"), NumberOfPieces = 22 },
                new Book { Id = Guid.Parse("f8a9b0c1-d2e3-4456-f789-a0b1c2d3e4f5").ToString(), Name = "Dve Veže", Author = "J.R.R. Tolkien", IssueYear = 2009, ISBN = GenerateValidIsbn13("978", "802490018"), NumberOfPieces = 20 },
                new Book { Id = Guid.Parse("a9b0c1d2-e3f4-4567-a890-b1c2d3e4f5a6").ToString(), Name = "Návrat Kráľa", Author = "J.R.R. Tolkien", IssueYear = 2009, ISBN = GenerateValidIsbn13("978", "802490019"), NumberOfPieces = 23 },
                new Book { Id = Guid.Parse("b0c1d2e3-f4a5-4678-b901-c2d3e4f5a6b7").ToString(), Name = "Silmarillion", Author = "J.R.R. Tolkien", IssueYear = 2010, ISBN = GenerateValidIsbn13("978", "802490020"), NumberOfPieces = 18 },
                new Book { Id = Guid.Parse("c1d2e3f4-a5b6-4789-c012-d3e4f5a6b7c8").ToString(), Name = "Kamenný Mlyn", Author = "J.K. Rowling", IssueYear = 2008, ISBN = GenerateValidIsbn13("978", "802490021"), NumberOfPieces = 30 },
                new Book { Id = Guid.Parse("d2e3f4a5-b6c7-4890-d123-e4f5a6b7c8d9").ToString(), Name = "Tajomná Komnata", Author = "J.K. Rowling", IssueYear = 2008, ISBN = GenerateValidIsbn13("978", "802490022"), NumberOfPieces = 28 },
                new Book { Id = Guid.Parse("e3f4a5b6-c7d8-4901-e234-f5a6b7c8d9e0").ToString(), Name = "Väzeň z Azkabanu", Author = "J.K. Rowling", IssueYear = 2008, ISBN = GenerateValidIsbn13("978", "802490023"), NumberOfPieces = 27 },
                new Book { Id = Guid.Parse("f4a5b6c7-d8e9-4012-f345-a6b7c8d9e0f1").ToString(), Name = "Ohnivá Čaša", Author = "J.K. Rowling", IssueYear = 2009, ISBN = GenerateValidIsbn13("978", "802490024"), NumberOfPieces = 26 },
                new Book { Id = Guid.Parse("a5b6c7d8-e9f0-4123-a456-b7c8d9e0f1a2").ToString(), Name = "Fénixov Rád", Author = "J.K. Rowling", IssueYear = 2009, ISBN = GenerateValidIsbn13("978", "802490025"), NumberOfPieces = 24 },
                new Book { Id = Guid.Parse("b6c7d8e9-f0a1-4234-b567-c8d9e0f1a2b3").ToString(), Name = "Polovičný Princ", Author = "J.K. Rowling", IssueYear = 2010, ISBN = GenerateValidIsbn13("978", "802490026"), NumberOfPieces = 29 },
                new Book { Id = Guid.Parse("c7d8e9f0-a1b2-4345-c678-d9e0f1a2b3c4").ToString(), Name = "Dary Smrti", Author = "J.K. Rowling", IssueYear = 2010, ISBN = GenerateValidIsbn13("978", "802490027"), NumberOfPieces = 25 },
                new Book { Id = Guid.Parse("d8e9f0a1-b2c3-4456-d789-e0f1a2b3c4d5").ToString(), Name = "Hra Prestolov", Author = "George R.R. Martin", IssueYear = 2011, ISBN = GenerateValidIsbn13("978", "802490028"), NumberOfPieces = 19 },
                new Book { Id = Guid.Parse("e9f0a1b2-c3d4-4567-e890-f1a2b3c4d5e6").ToString(), Name = "Stretnutie Kráľov", Author = "George R.R. Martin", IssueYear = 2011, ISBN = GenerateValidIsbn13("978", "802490029"), NumberOfPieces = 17 },
                new Book { Id = Guid.Parse("f0a1b2c3-d4e5-4678-f901-a2b3c4d5e6f7").ToString(), Name = "Búrka Mečov", Author = "George R.R. Martin", IssueYear = 2012, ISBN = GenerateValidIsbn13("978", "802490030"), NumberOfPieces = 20 },
                new Book { Id = Guid.Parse("9a8b7c6d-5e4f-3a21-9876-543210fedcba").ToString(), Name = "Hostina Vodcov", Author = "George R.R. Martin", IssueYear = 2012, ISBN = GenerateValidIsbn13("978", "802490031"), NumberOfPieces = 18 },
                new Book { Id = Guid.Parse("8b7a6c5d-4e3f-2a10-8765-432109edcba9").ToString(), Name = "Tanec S Drakami", Author = "George R.R. Martin", IssueYear = 2013, ISBN = GenerateValidIsbn13("978", "802490032"), NumberOfPieces = 16 },
                new Book { Id = Guid.Parse("7a6b5c4d-3e2f-1a09-7654-321098dcba87").ToString(), Name = "Mágov Učeň", Author = "Terry Pratchett", IssueYear = 2010, ISBN = GenerateValidIsbn13("978", "802490033"), NumberOfPieces = 21 },
                new Book { Id = Guid.Parse("6b5a4c3d-2e1f-0a98-6543-210987cba765").ToString(), Name = "Svetlo Fantastické", Author = "Terry Pratchett", IssueYear = 2011, ISBN = GenerateValidIsbn13("978", "802490034"), NumberOfPieces = 22 },
                new Book { Id = Guid.Parse("5a4b3c2d-1e0f-9a87-5432-109876ba5434").ToString(), Name = "Strážci! Strážci!", Author = "Terry Pratchett", IssueYear = 2012, ISBN = GenerateValidIsbn13("978", "802490035"), NumberOfPieces = 20 },
                new Book { Id = Guid.Parse("4a3b2c1d-0e9f-8a76-4321-098765a4321f").ToString(), Name = "Mort", Author = "Terry Pratchett", IssueYear = 2010, ISBN = GenerateValidIsbn13("978", "802490036"), NumberOfPieces = 19 },
                new Book { Id = Guid.Parse("3a2b1c0d-9e8f-7a65-3210-9876543210ef").ToString(), Name = "Pyramídy", Author = "Terry Pratchett", IssueYear = 2011, ISBN = GenerateValidIsbn13("978", "802490037"), NumberOfPieces = 23 },
                new Book { Id = Guid.Parse("2a1b0c9d-8e7f-6a54-2109-8765432109de").ToString(), Name = "Očný Blesk", Author = "Brandon Sanderson", IssueYear = 2015, ISBN = GenerateValidIsbn13("978", "802490038"), NumberOfPieces = 15 },
                new Book { Id = Guid.Parse("1a0b9c8d-7e6f-5a43-1098-7654321098cd").ToString(), Name = "Vzostup Čestného", Author = "Brandon Sanderson", IssueYear = 2016, ISBN = GenerateValidIsbn13("978", "802490039"), NumberOfPieces = 14 },
                new Book { Id = Guid.Parse("0a9b8c7d-6e5f-4a32-0987-6543210987bc").ToString(), Name = "Slovo Za Svietka", Author = "Brandon Sanderson", IssueYear = 2017, ISBN = GenerateValidIsbn13("978", "802490040"), NumberOfPieces = 16 },
                new Book { Id = Guid.Parse("9f8e7d6c-5b4a-3c21-9876-543210fedcb9").ToString(), Name = "Cestovanie Života", Author = "Brandon Sanderson", IssueYear = 2018, ISBN = GenerateValidIsbn13("978", "802490041"), NumberOfPieces = 13 },
                new Book { Id = Guid.Parse("8e7d6c5b-4a39-2c10-8765-432109edcb8a").ToString(), Name = "Poslucháč", Author = "Andrzej Sapkowski", IssueYear = 2009, ISBN = GenerateValidIsbn13("978", "802490042"), NumberOfPieces = 17 },
                new Book { Id = Guid.Parse("7d6c5b4a-3928-1c09-7654-321098dcb789").ToString(), Name = "Meč Osudu", Author = "Andrzej Sapkowski", IssueYear = 2009, ISBN = GenerateValidIsbn13("978", "802490043"), NumberOfPieces = 18 },
                new Book { Id = Guid.Parse("6c5b4a39-2817-0c98-6543-210987cb6789").ToString(), Name = "Posledné Prianie", Author = "Andrzej Sapkowski", IssueYear = 2010, ISBN = GenerateValidIsbn13("978", "802490044"), NumberOfPieces = 19 },
                new Book { Id = Guid.Parse("5b4a3928-1706-9c87-5432-109876ba5678").ToString(), Name = "Krví Elfa", Author = "Andrzej Sapkowski", IssueYear = 2010, ISBN = GenerateValidIsbn13("978", "802490045"), NumberOfPieces = 16 },
                new Book { Id = Guid.Parse("4a392817-0605-8c76-4321-098765a45678").ToString(), Name = "Hodina Pohanstva", Author = "Andrzej Sapkowski", IssueYear = 2011, ISBN = GenerateValidIsbn13("978", "802490046"), NumberOfPieces = 20 },
                new Book { Id = Guid.Parse("39281706-0504-7c65-3210-98765434567f").ToString(), Name = "Stav Panteónu", Author = "Neil Gaiman", IssueYear = 2012, ISBN = GenerateValidIsbn13("978", "802490047"), NumberOfPieces = 14 },
                new Book { Id = Guid.Parse("28170605-0403-6c54-2109-87654323456e").ToString(), Name = "Američania Bohovia", Author = "Neil Gaiman", IssueYear = 2013, ISBN = GenerateValidIsbn13("978", "802490048"), NumberOfPieces = 15 },
                new Book { Id = Guid.Parse("17060504-0302-5c43-1098-76543212345d").ToString(), Name = "Zlatý Kompas", Author = "Philip Pullman", IssueYear = 2010, ISBN = GenerateValidIsbn13("978", "802490049"), NumberOfPieces = 21 },
                new Book { Id = Guid.Parse("06050403-0201-4c32-0987-65432101234c").ToString(), Name = "Tajný Nástroj", Author = "Philip Pullman", IssueYear = 2011, ISBN = GenerateValidIsbn13("978", "802490050"), NumberOfPieces = 22 },
                new Book { Id = Guid.Parse("05040302-0100-3c21-9876-54321090123b").ToString(), Name = "Briliantový Mikroskop", Author = "Philip Pullman", IssueYear = 2012, ISBN = GenerateValidIsbn13("978", "802490051"), NumberOfPieces = 20 },
                new Book { Id = Guid.Parse("04030201-0009-2c10-8765-43210989012a").ToString(), Name = "Čarodejník z Londýna", Author = "Susanna Clarke", IssueYear = 2014, ISBN = GenerateValidIsbn13("978", "802490052"), NumberOfPieces = 12 },
                new Book { Id = Guid.Parse("03020100-0908-1c09-7654-32109878901a").ToString(), Name = "Imaginárne Živé", Author = "China Miéville", IssueYear = 2013, ISBN = GenerateValidIsbn13("978", "802490053"), NumberOfPieces = 13 }
            };

            await _context.Books.AddRangeAsync(books, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded {Count} books successfully", books.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding books");
            throw;
        }
    }
}

