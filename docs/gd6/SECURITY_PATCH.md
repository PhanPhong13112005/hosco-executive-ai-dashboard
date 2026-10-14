# Vá dependency và giới hạn bảo mật GD6
Cleanclone14/10/2026 phát hiện NU1902/NU1903 ở Pkcs9.0.11/10.0.11 vàXml10.0.11. Đối chiếu Microsoft advisory published13/10:
- [Pkcs High CVE-2026-84008](https://github.com/advisories/GHSA-846f-f764-76mx).
- [Pkcs Moderate CVE-2026-70162](https://github.com/advisories/GHSA-2fmv-rv2c-59x7).
- [XML CVE-2026-96960](https://github.com/advisories/GHSA-7hqp-7c5m-4vq6).
- [XML CVE-2026-96959](https://github.com/advisories/GHSA-c399-9853-6jmj).
- [XML CVE-2026-96958](https://github.com/advisories/GHSA-r955-8q5j-3h83).
Pin trực tiếp Pkcs/Xml10.0.13 ở Infrastructure; áp patch version được advisorychỉđịnh, tránh transitive9.0.11quaSqlClient/design dependencies. Không broadupgradeEF/PDFsharp, khôngsuppressNuGetAudit. Restore/build0warnings vàNuGetauditfreshkhông báo vulnerable graph; fullregression được chạy lại.
**Machine shared framework .NET/ASP.NET10.0.11 chưa được cập nhật.** NuGetaudit chỉ kiểm projectdependencygraph, không chứng minh runtime/System.Private.Xml đãvá. IT cần phêduyệt update/trusteddeployment trước môi trườngproduction. KhôngreinstallSDK/disableWDAC hoặc đưa privatekeyproduction vàorepo.
Secretreview chỉscanGD6changedfiles/evidence; khôngclaimpentest toànhệthống. Credentialsdemođãcósourcekhácsecretthật. Screenshotchỉsyntheticdata; khônglưuJWT/Authorization/login request.
