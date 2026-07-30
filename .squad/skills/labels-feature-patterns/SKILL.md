# Labels Feature Patterns (Sprint 6)

**Confidence:** medium
**Last validated:** 2026-04-02 (Sprint 6 implementation)

## Data Model

- Labels stored as `List<string>` on Issue model — MongoDB persists as array field
- No separate Labels collection — simple embedded list
- IssueDto is a positional record — when adding new fields, update: BunitTestBase.CreateTestIssue(), IssueMapperTests, IssueServiceTests, NotificationServiceTests, DashboardServiceTests, IssueDto.Empty

## Service Layer

- ILabelService / LabelService provide label suggestions from existing issues
- Simple prefix-match aggregation across all Issue.Labels fields
- GET /api/labels/suggestions?prefix={query} endpoint

## CQRS Handlers

- AddLabelCommand: appends label (no-op if duplicate)
- RemoveLabelCommand: removes label by value
- Both publish IssueUpdatedEvent via MediatR, return Result<IssueDto>

## Frontend

- LabelInput.razor: multi-value tag input
  - 300ms debounced autocomplete calling ILabelService
  - Comma or Enter key confirms a tag
  - Backspace removes last tag
  - ValueChanged callback propagates List<string> to parent form
- Filter chips: URL query param (?label=bug,v2) drives filter
  - NavigationManager integration for URL-state sync
  - Clicking chip toggles filter on/off

## Testing

- FakeNavigationManager MUST override NavigateToCore(string, NavigationOptions) for bUnit tests involving navigation
- Test counts after Sprint 6: 716 bUnit | 404 domain | 60 arch | 1,167 total
