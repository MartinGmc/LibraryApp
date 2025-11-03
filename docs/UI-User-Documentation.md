# Library Management System - User Guide

## Welcome

This guide will help you use the Library Management System to browse books, borrow and return books, and manage your library account.

---

## Getting Started

### Accessing the System

1. Open your web browser and navigate to the Library Management System URL (provided by your administrator)
2. You will be automatically redirected to the **Login** page if you're not already logged in

### Logging In

1. On the **Login** page, enter your:
   - **Login**: Your username
   - **Password**: Your password
2. Click the **Login** button
3. If your credentials are correct, you'll be redirected to the main Library page
4. If incorrect, you'll see an error message - check your credentials and try again

**Note:** Your session will remain active for 8 hours. You'll need to log in again after that time.

---

## Main Library Page

After logging in, you'll see the main Library page with the following features:

### Page Layout

The page is divided into several sections:

1. **Navigation Bar** (top):
   - **Library App** title on the left
   - Your username displayed in the center-right
   - **Log Out** button (red outlined) on the right - Click to log out of the system

2. **Header Buttons** (below navigation bar, top right):
   - **My Borrowed Books** button (blue) - View all books you currently have borrowed
   - **New Book** button (primary) - Add a new book to the library catalog

3. **Search Filters** (below header):
   - **Filter by Name** - Search for books by title
   - **Filter by Author** - Search for books by author name
   - **Filter by ISBN** - Search for books by ISBN number

4. **Books Table** - Displays the list of books with:
   - Book name
   - Author
   - Issue year
   - ISBN
   - Number of pieces (total copies)
   - Available to borrow (current availability)
   - Action buttons (Borrow/Return)

5. **Pagination Controls** (bottom):
   - Shows current page number and total pages
   - **Previous** and **Next** buttons to navigate between pages

---

## Searching for Books

### Using Filters

The system provides three ways to search for books:

#### 1. Filter by Book Name

- Type in the **Filter by Name** field to search by book title
- **Autocomplete**: After typing at least 4 characters, suggestions will appear
- Click on a suggestion to auto-fill the field
- The search is **case-insensitive** and matches partial text
- Results update automatically after you stop typing (500ms delay)

**Example:** Typing "harry" will find "Harry Potter and the Philosopher's Stone"

#### 2. Filter by Author

- Type in the **Filter by Author** field to search by author name
- **Autocomplete**: After typing at least 4 characters, suggestions will appear
- Click on a suggestion to auto-fill the field
- The search is **case-insensitive** and matches partial text
- Results update automatically after you stop typing

**Example:** Typing "rowling" will find all books by "J.K. Rowling"

#### 3. Filter by ISBN

- Type in the **Filter by ISBN** field to search by ISBN number
- The field automatically formats ISBN-13 numbers with hyphens (e.g., `978-0-123456-78-9`)
- You can enter digits with or without hyphens
- Results update automatically after you stop typing

**Example:** Typing "9780743273565" or "978-0-7432-7356-5" will find the book with that ISBN

### Combining Filters

You can use multiple filters at the same time to narrow down your search:
- The system will show only books that match **all** active filters
- Clear a filter field to remove that filter from your search

### Clearing Filters

To clear all filters and see all books:
- Delete the text in each filter field
- The book list will automatically refresh to show all books

---

## Viewing Books

### Books Table

The books table displays the following information for each book:

| Column | Description |
|--------|-------------|
| **Name** | The title of the book |
| **Author** | The author's name |
| **Issue Year** | The year the book was published |
| **ISBN** | The ISBN-13 identifier (formatted with hyphens) |
| **Number of Pieces** | Total number of copies the library owns |
| **Available to borrow** | Current number of copies available (updated in real-time) |
| **Actions** | Borrow and Return buttons |

### Pagination

- Books are displayed **10 per page** by default
- Use the **Previous** and **Next** buttons at the bottom to navigate between pages
- The pagination shows:
  - Current page number
  - Total number of pages
  - Range of books displayed (e.g., "Showing 1 to 10 of 25 books")

### Understanding Availability

- **Available to borrow**: Shows how many copies are currently available
- **Number of Pieces**: Shows the total copies the library owns
- If "Available to borrow" is **0**, the book cannot be borrowed (all copies are currently loaned out)
- If "Available to borrow" is greater than **0**, you can borrow the book

---

## Borrowing Books

### How to Borrow

1. Find the book you want to borrow in the books table
2. Check the **Available to borrow** column - it must be greater than 0
3. Click the green **Borrow** button for that book
4. The button will show a loading spinner while processing
5. Once successful:
   - The **Available to borrow** count will decrease
   - The **Borrow** button will become disabled (gray) if no copies remain
   - The **Return** button will become enabled (yellow) for your borrowed copy

### Borrowing Rules

- You can borrow a book only if there are available copies
- You can borrow the same book multiple times (if multiple copies are available)
- The system tracks which books you've borrowed
- You can see all your borrowed books by clicking **My Borrowed Books**

### What Happens When You Borrow

- A loan record is created with the current date and time
- The available count decreases immediately
- The book appears in your "My Borrowed Books" list
- Other users see the updated availability in real-time

---

## Returning Books

### How to Return

1. Find the book you want to return in the books table
2. If you have borrowed the book, the **Return** button will be yellow and enabled
3. Click the **Return** button
4. The button will show a loading spinner while processing
5. Once successful:
   - The **Available to borrow** count will increase
   - The **Return** button will become disabled (gray) if you don't have any more copies
   - The **Borrow** button will become enabled again if copies are available

### Return Rules

- You can only return books that you have currently borrowed
- If you borrowed multiple copies of the same book, returning one will:
  - Return the oldest loan first (FIFO - First In, First Out)
  - Decrease your active loan count by 1
- The book must still exist in the library catalog to be returned

### What Happens When You Return

- Your loan record is marked as returned with the current date and time
- The available count increases immediately
- The book is removed from your "My Borrowed Books" list
- Other users see the updated availability in real-time

---

## My Borrowed Books

### Viewing Your Borrowed Books

1. Click the **My Borrowed Books** button (blue button with book icon) in the top right
2. A modal window will open showing all books you currently have borrowed
3. The table displays:
   - **Name**: Book title
   - **Author**: Author name
   - **Issue Year**: Publication year
   - **ISBN**: ISBN-13 identifier
   - **Borrowed Date**: Date and time when you borrowed the book

### Features

- Shows only books you currently have borrowed (not yet returned)
- Displays the exact date and time when each book was borrowed
- Click **Close** or click outside the modal to close it
- The list updates automatically when you borrow or return books

### Empty List

If you don't have any borrowed books, the modal will show:
> "You don't have any borrowed books at the moment."

---

## Adding New Books

### How to Add a Book

1. Click the **New Book** button (primary button with plus icon) in the top right
2. A modal window will open with a form
3. Fill in all required fields:
   - **Name** (required): Book title (max 300 characters)
   - **Author** (required): Author name (max 200 characters)
   - **Issue Year** (required): Publication year (between 1000 and current year)
   - **ISBN** (required): ISBN-13 format (13 digits)
   - **Number of Pieces** (required): Number of physical copies (must be 0 or greater)
4. Click **Add Book** to submit

### ISBN Validation

The system validates ISBN-13 format in real-time:
- As you type, the ISBN field automatically formats with hyphens (e.g., `978-0-123456-78-9`)
- A **green checkmark** (✓) appears when the ISBN is valid
- A **red X** appears when the ISBN is invalid
- You must enter a valid ISBN-13 to add the book

**ISBN Format:** ISBN-13 should be 13 digits, optionally with hyphens (e.g., `978-0-123456-78-9` or `9780123456789`)

### Validation Rules

All fields are validated before submission:

| Field | Requirements |
|-------|--------------|
| **Name** | Required, max 300 characters |
| **Author** | Required, max 200 characters |
| **Issue Year** | Required, between 1000 and current year |
| **ISBN** | Required, valid ISBN-13 format, max 50 characters |
| **Number of Pieces** | Required, must be 0 or greater |

### Success and Errors

**Success:**
- The modal closes automatically
- The new book appears in the book list
- The book list refreshes to show the new book

**Error Messages:**
- **Validation errors**: Red text appears showing which fields have errors
- **Duplicate book**: If a book with the same Name + Author + ISBN combination already exists, you'll see an error message
- **Invalid ISBN**: The ISBN validation indicator will show red, and you'll see an error message

### Closing the Form

- Click **Cancel** to close without saving
- Click the **X** button in the top right corner
- Click outside the modal (on the dark background)

---

## Troubleshooting

### Common Issues

#### "No books found matching your filters"
- **Solution**: Clear your filters or adjust your search terms
- The book you're looking for might not be in the catalog

#### "No available copies of this book"
- **Solution**: All copies are currently borrowed
- Wait until someone returns a copy, or try a different book

#### "You don't have an active loan for this book"
- **Solution**: You can only return books you've borrowed
- Check "My Borrowed Books" to see what you currently have

#### "A book with this combination already exists"
- **Solution**: The book (same Name + Author + ISBN) is already in the catalog
- Search for it in the book list instead of adding it again

#### ISBN validation fails
- **Solution**: Ensure you're entering a valid ISBN-13 (13 digits)
- Check that the check digit is correct
- The field will show a red X if invalid

#### Cannot log in
- **Solution**: Check your username and password
- Contact your administrator if you've forgotten your credentials
- Ensure you're using the correct login credentials

#### Page not loading or errors
- **Solution**: Refresh the page (F5 or Ctrl+R)
- Check your internet connection
- Try logging out and logging back in
- Contact support if the problem persists

---

## Tips and Best Practices

### Search Tips

1. **Use autocomplete**: Type at least 4 characters to see suggestions - this helps you find books faster
2. **Combine filters**: Use multiple filters together for precise searches
3. **Clear filters**: Remove all filters to see the complete book catalog

### Borrowing Tips

1. **Check availability**: Always check the "Available to borrow" count before borrowing
2. **Borrow responsibly**: Return books when you're done so others can use them
3. **Track your loans**: Use "My Borrowed Books" to see what you have borrowed

### ISBN Tips

1. **Auto-formatting**: The system automatically formats ISBNs with hyphens as you type
2. **Validation**: Look for the green checkmark (✓) to confirm your ISBN is valid
3. **Format**: ISBN-13 format is 13 digits, optionally with hyphens

### General Tips

1. **Real-time updates**: The system updates in real-time - availability changes immediately
2. **Pagination**: Use pagination to browse through large catalogs efficiently
3. **Session**: Your login session lasts 8 hours - you'll need to log in again after that

---

## Keyboard Shortcuts

- **Escape (Esc)**: Close autocomplete suggestions when typing in filter fields
- **Tab**: Navigate between form fields
- **Enter**: Submit forms (login, add book)

---

## Browser Compatibility

The Library Management System works best with:
- **Chrome** (recommended)
- **Edge**
- **Firefox**
- **Safari**

**Note:** JavaScript must be enabled for the system to function properly.

---

## Getting Help

If you need assistance:

1. **Check this guide** for solutions to common issues
2. **Contact your administrator** for account-related issues
3. **Report bugs** to the system administrator with details about:
   - What you were trying to do
   - What happened instead
   - Any error messages you saw

---

## Logging Out

To log out of the system:

1. Click the **Log Out** button in the top navigation bar (red outlined button with logout icon)
2. The button will show a loading spinner while processing
3. You will be automatically redirected to the Login page
4. Your session will be cleared immediately

**Note:** The Log Out button is located in the top right corner of the navigation bar, next to your username. You can also close your browser window/tab, but using the Log Out button is recommended for proper session cleanup.

---

## Security Notes

- **Never share your login credentials** with anyone
- **Log out** when using a shared computer
- **Report suspicious activity** to your administrator immediately
- The system uses secure authentication - your password is never displayed

---

## Summary of Features

✅ **Browse Books**: Search and filter the library catalog  
✅ **Borrow Books**: Borrow available books with one click  
✅ **Return Books**: Return borrowed books easily  
✅ **Track Loans**: View all your currently borrowed books  
✅ **Add Books**: Add new books to the library catalog  
✅ **Real-time Updates**: See availability changes immediately  
✅ **Autocomplete**: Quick search with suggestions  
✅ **Pagination**: Navigate through large catalogs efficiently  

---

**Thank you for using the Library Management System!**

