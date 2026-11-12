$log = Get-ChildItem "$env:USERPROFILE\.android\avd\pixel_7_-_api_36_0.avd\logs" -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 5
$log