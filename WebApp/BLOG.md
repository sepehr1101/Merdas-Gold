# Merdas blog

Admin: `/admin/content/blog`; public listing: `/blog`; article: `/blog/{slug}`.

- Administrators create and edit a cover image, title, summary, stable unique slug, and a structured rich-text body. Drafts can omit text/image; publishing requires both.
- The editor supports paragraphs, H2/H3, bold, ordered/unordered lists, links, undo/redo and plain-text paste. No inline images, font/color controls or raw HTML are accepted. The server renders an allowlisted node tree with encoded text and attributes. Client and server limit document size; the circuit message limit accommodates the editor payload.
- Images are limited to 5 MB with JPEG/PNG/WebP signature checks. The public image endpoint only returns published images; admins can view drafts. Responses are no-store so unpublishing does not leave a publicly cached cover.
- Unique slugs and rowversion prevent duplicate addresses and silent concurrent overwrites. Save failures keep the form open. Unpublishing removes articles from public queries without deleting content. The summary doubles as the meta description.
- Migration `AddBlogPosts` creates the blog table. Existing product and pricing rules are unchanged.

## Initial articles

Run against the intended environment after deploying:

```powershell
dotnet run --project WebApp -- --seed-blog
```

The dedicated seed command creates three published Persian articles using the existing storefront blog images. It skips existing slugs and never overwrites edited posts. It is not run on ordinary startup. On this development workspace it has already been executed successfully (3 posts).

## Verification

`dotnet test Tests/MerdasGold.Tests/MerdasGold.Tests.csproj` covers document encoding, invalid node types, unsafe links, draft/publish validation, slugs and image rejection, alongside the existing suite. Browser checks cover the listing, edit/save round-trip, unpublish and public visibility, and responsive article views.
