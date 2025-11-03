namespace LibraryApp.Application.Books.Services;

public class IsbnValidationService : IIsbnValidationService
{
    public bool ValidateIsbn13(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return false;

        // Remove hyphens and spaces for validation
        var cleanedIsbn = isbn.Replace("-", "").Replace(" ", "");

        // Check if it's ISBN-13 (13 digits)
        if (cleanedIsbn.Length != 13)
            return false;

        // Check that all are digits
        for (int i = 0; i < 13; i++)
        {
            if (!char.IsDigit(cleanedIsbn[i]))
                return false;
        }

        // Calculate ISBN-13 check digit (EAN-13 algorithm)
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = int.Parse(cleanedIsbn[i].ToString());
            int weight = (i % 2 == 0) ? 1 : 3;
            sum += digit * weight;
        }

        int checkDigit = (10 - (sum % 10)) % 10;
        int actualCheckDigit = int.Parse(cleanedIsbn[12].ToString());

        return actualCheckDigit == checkDigit;
    }
}

