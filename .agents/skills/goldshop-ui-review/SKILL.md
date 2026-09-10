---
name: goldshop-ui-review
description: Create, review, or refactor UI specifically in the MerdasGold Persian RTL gold-shop project, including Blazor pages/components, storefront and admin layouts, forms, tables, responsive behavior, and design-system consistency. Do not use for backend-only, database, API, or business-logic work.
---

# MerdasGold UI

Maintain a coherent, production-quality interface rather than polishing screens in isolation. User instructions take precedence.

## Product direction

- Storefront: premium, calm, contemporary, trustworthy, and product-focused. Let jewelry imagery lead. Avoid dashboard styling, exaggerated luxury effects, and generic AI landing-page patterns.
- Admin: professional, predictable, readable, and compact but comfortable. Never gain density by making ordinary text or controls too small.
- Persian and RTL are the default. Visually check mixed Persian/Latin content; keep codes, SKUs, phone numbers, URLs, and similar technical values LTR where appropriate.

## Work from the system

Before adding a local CSS value or component, inspect the existing theme/tokens, shared components, layouts, and representative responsive rules.

1. Reuse an existing semantic token or shared component when suitable.
2. When a recurring need is genuinely missing, add or normalize it centrally.
3. Propagate the shared correction only to affected surfaces; avoid unrelated redesigns.
4. Do not introduce a second token system.

Centralize typography, spacing, control heights, radii, borders, shadows, colors, content widths, breakpoints, and layering as needed. Prefer the project's established values. If none exist, use a small deliberate scale; avoid arbitrary intermediate values.

## Non-negotiable design rules

- Preserve readable Persian typography. Normal descriptions, labels, table values, buttons, prices, and validation messages are not caption text.
- Use spacing to show relationships: tight within a group, moderate between groups, generous between sections.
- Keep equivalent controls the same height. Ordinary admin controls should feel compact but comfortable; customer-facing controls may be slightly more touch-friendly.
- Cards are functional containers, not the default wrapper. Avoid excessive padding, minimum heights, radius, shadows, nested cards, and empty space.
- Prefer restrained borders, backgrounds, proportion, typography, photography, and alignment over gradients, glass, glow, decorative blobs, floating cards, excessive pills, or gratuitous animation.
- Forms share label, field, helper/error, focus, disabled, grouping, and action patterns. Never use placeholders as the only label.
- Tables prioritize scanning: consistent headers, rows, padding, alignment, actions, loading/empty states, filters, sorting, and pagination. Repeated row actions must not dominate.
- Product UI prioritizes image, name, price, important attributes, and purchase action—in that order. Listing cards show only what aids scanning and comparison.
- One primary action should be visually clear per logical section; secondary actions must not compete with it.
- Preserve existing brand and functioning workflows unless the user asks for a redesign.

## Implementation boundaries

- Prefer reusable Razor components and existing styling primitives over duplicated markup.
- Keep presentation logic in components and business rules outside them.
- Do not silently change APIs, authorization, routing, calculations, persistence, or other behavior during UI work.
- Avoid new JavaScript dependencies when Blazor handles the interaction cleanly; use interop only for a clear practical benefit.
- Account for relevant default, hover, focus, active, disabled, loading, error, empty, and selected states.
- Maintain contrast, visible focus, keyboard access, semantic labels, accessible names, error identification, alt text, and useful touch targets. Do not convey state by color alone.
- Design narrow layouts deliberately rather than shrinking desktop UI. Prevent overflow, restructure toolbars, stack groups when useful, and preserve typography and touch targets. Admin tables may scroll horizontally when that is the clearest solution.

## Workflow and completion

Inspect before editing, identify the systemic cause, normalize shared rules first, then apply the smallest coherent change. Validate the actual Persian RTL UI at desktop and mobile sizes plus any task-relevant intermediate viewport. After structural UI changes, run the normal build and relevant tests.

Before finishing, confirm consistent typography, spacing, sizing, RTL behavior, responsiveness, reuse, accessibility, and unchanged business behavior. Summarize systemic fixes, shared tokens/components changed, representative surfaces checked, and any remaining inconsistency—briefly.

For an explicit comprehensive UI review or design-system audit, read [references/review-checklist.md](references/review-checklist.md). Do not load it for a small, well-scoped UI edit.

