# Dashboard PDF font

NotoSans-Regular.ttf is the unmodified static Noto Sans font from
https://github.com/notofonts/noto-fonts/blob/main/hinted/ttf/NotoSans/NotoSans-Regular.ttf.
The accompanying LICENSE.txt is the upstream SIL Open Font License 1.1.

Embed this resource so PDF export has Vietnamese glyphs on every host without
depending on Windows fonts or downloading fonts at runtime.

SHA-256: `B85C38ECEA8A7CFB39C24E395A4007474FA5A4FC864F6EE33309EB4948D232D5`.
PDF serialization uses PDFsharp 6.2.2 (MIT), pinned in the infrastructure project:
https://www.nuget.org/packages/PDFsharp/6.2.2 and
https://docs.pdfsharp.net/PDFsharp/Topics/Fonts/Font-Resolving.html.
