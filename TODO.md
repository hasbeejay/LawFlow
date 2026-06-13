# LawFlow DB Performance Optimization - TODO

## Step 1: Add SQL-side paged filtering APIs in `CaseService`
- [x] Add method for paged cases for a user/role with:
  - optional search text (server-side)
  - optional status filter
  - pagination via `Skip/Take`
- [x] Avoid `.ToLower().Contains()`; use `EF.Functions.ILike`/translatable patterns.



## Step 2: Update cases pages to use paged API
- Update `Components/Pages/Cases.razor`
  - remove `GetCasesForUserAsync` full load
  - implement `MudTable` server paging (load per page from DB)
- Update `Components/Pages/Judge/Caseload.razor`
- Update `Components/Pages/Police/Caseload.razor`

## Step 3: Optimize `/admin/assignments`
- Add `CaseService` method that fetches only cases awaiting assignment (Statuses: Created, ReviewedByAdmin)
- Update `Components/Pages/Admin/Assignments.razor` to use that method (no `GetAllCasesAsync` + includes).

## Step 4: Rewrite dashboard trends query
- Rewrite `DashboardService.GetCaseCompletionTrendsAsync()` to perform aggregation in SQL.

## Step 5: Add/verify indexes
- Add indexes for common query patterns in `ApplicationDbContext`.
- If migrations are used, generate/apply.

## Step 6: Testing
- Open slow pages again and confirm improvements.
- If still slow, capture and further optimize the remaining slow query paths.

