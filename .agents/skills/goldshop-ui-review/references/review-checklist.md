# Comprehensive UI review checklist

Read this reference only for a broad UI review, redesign assessment, or design-system audit.

## Inspect representative surfaces

- Global theme, CSS tokens, shared layout, navigation, and shared components
- One dashboard/overview, table-heavy page, form, detail page, and card/grid page
- Storefront listing/detail surface when present
- Desktop, tablet where behavior changes, and mobile

## Detect systemic drift

- Too many font sizes or ordinary text reduced to caption size
- Duplicate colors/gray tones, random spacing, inconsistent control heights
- Inconsistent card padding, radii, borders, shadows, or oversized empty containers
- Page-specific layout hacks, repeated markup, and avoidable magic values
- Weak hierarchy or multiple competing primary actions
- RTL direction errors and mixed-direction strings rendered incorrectly
- Overflow, desktop-only toolbars, unusable tables, or undersized mobile targets
- Missing focus, loading, error, empty, disabled, and selected states

Treat repeated occurrences caused by one missing token or primitive as one systemic finding.

## Fallback scales

Use these only when the project lacks an established equivalent. They are starting ranges, not mandatory replacements for good existing tokens.

- Typography: hero 32–40px; page title 24–28px; section 19–22px; card 16–18px; body 15–16px; small body/label 14px; helper 13px; true caption 12px.
- Spacing: 4, 8, 12, 16, 20, 24, 32, 40, 48, 64px.
- Controls: small secondary 32–34px; default admin 38–42px; comfortable/storefront 42–46px. Avoid 50px+ controls without a deliberate reason.
- Radius: small 6px; default 8px; medium 12px; large 16px. Large radii and strong shadows require a clear visual purpose.

## Severity

- Blocking: an important viewport or common task is broken, unreadable, inaccessible, or unusable.
- Major: a significant hierarchy, consistency, responsive, or usability defect affects an important flow.
- Minor: worthwhile polish that does not impede task completion.

Prioritize systemic and user-impacting findings over pixel-level polish.

## Completion evidence

- Shared system correction precedes local overrides where appropriate.
- Similar components use compatible typography, spacing, and dimensions.
- Cards size to content and do not wrap every group unnecessarily.
- Forms and tables follow their shared interaction patterns.
- Storefront keeps products and purchase information visually primary.
- Persian RTL and mixed-direction content are visually verified.
- Responsive layouts remain readable and actionable without accidental overflow.
- Business behavior remains unchanged unless explicitly in scope.
- Build and relevant tests pass, or any pre-existing/unrelated failure is reported.

