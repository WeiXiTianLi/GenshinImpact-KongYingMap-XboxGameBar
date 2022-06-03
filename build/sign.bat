@echo off
set certfile=../空荧酒馆-悬浮窗_TemporaryKey.pfx
set /p certpass=<../空荧酒馆-悬浮窗_TemporaryKey.pfxpass
echo Signing %1
signtool sign /f %certfile% /p %certpass% /fd SHA256 /t http://timestamp.digicert.com %1