---
name: clean-markdown
description: Formats, rewrites, or generates Markdown documents adhering strictly to markdownlint standards. Use when the user asks to format, organize, structure, or clean up Markdown files, design docs, specifications, or technical documentation without redundant bolding, horizontal rules, or arbitrary section numbers.
disable-model-invocation: true
---
# Markdown Output Rules (Strict)

When generating Markdown documents, strictly adhere to the following rules to conform with markdownlint:

1. **No Redundant Emphasis**:
   - Do NOT overuse bold (`**`) or italic formatting inside sentences or bullet points.
   - Use bold only for critical keywords, status indicators, or table headers.

2. **No Horizontal Rules**:
   - Do NOT insert divider lines (`---` or `***`) between sections. Use heading levels for structural separation.

3. **No Automatic Section Numbering**:
   - Do NOT prepend arbitrary sequential numbers to headings (e.g., use `## Architecture`, NEVER `## 1. Architecture` or `## 1.1`).

4. **Lists & Lint Compliance**:
   - **List Markers**:
     - For unordered lists, strictly use `-` (do NOT use `*` or `+`) (MD004).
     - For ordered lists, use incremental sequential numbering (`1.`, `2.`, `3.`) starting at 1 (MD029).
   - **Nested & Mixed Lists**:
     - Both ordered and unordered lists are permitted at any nesting level (e.g., ordered sub-items under an unordered parent, or numbered sub-steps under a numbered step).
     - Select list type based on semantic meaning: use ordered lists when items represent sequential steps, ranking, or enumerated arguments; use unordered lists when items are non-sequential.
   - **Indentation & Spacing**:
     - Indent nested lists consistently with exactly 2 or 4 spaces matching the parent list style (MD005, MD007).
     - Leave exactly one blank line before and after list blocks, code blocks, and headings (MD022, MD031, MD032).
     - Ensure a single space after `#` in headings (MD018).
     - Do not leave trailing spaces at the end of lines (MD009).

5. **Direct Output**:
   - Output pure Markdown directly without conversational greetings, meta-announcements, or decorative prefixes.