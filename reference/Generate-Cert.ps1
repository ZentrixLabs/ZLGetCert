# Create new Certificate Request for Windows CA
# Requires -RunAsAdministrator

# Set location of the server
$Location = Read-Host "Enter City"
$State    = Read-Host "Enter State Abbreviation"
$OU       = "IT"
$Company  = "root.mpmaterials.com"

# Base folder
$CertFolder = "C:\ssl"
if (!(Test-Path $CertFolder)) { New-Item -Path $CertFolder -Type Directory | Out-Null }
Set-Location $CertFolder

# Names
$MachineName  = Read-Host "Enter Host Name"
$FQDN         = "$MachineName.root.mpmaterials.com"
$CertName     = $FQDN
$FriendlyName = $MachineName
$Subject      = "CN=$CertName, OU=$OU, O=$Company, L=$Location, S=$State, C=US"

# --- Collect SANs (unlimited) ---
# Always include machine short name + FQDN
$dnsEntries = @($MachineName, $FQDN)

# Optional additional DNS SANs
$idx = 1
while ($true) {
    $dnsInput = Read-Host "Enter additional DNS SAN #$idx (leave blank to finish)"
    if ([string]::IsNullOrWhiteSpace($dnsInput)) { break }
    $dnsEntries += $dnsInput.Trim()
    $idx++
}

# At least one IP (your original script asked for one)
$ipEntries = @()
$IPv4Address = Read-Host "Enter IP Address (leave blank to skip)"
if (-not [string]::IsNullOrWhiteSpace($IPv4Address)) {
    $ipEntries += $IPv4Address.Trim()
}

# Optional additional IP SANs
$ipIdx = 1
while ($true) {
    $ipInput = Read-Host "Enter additional IP SAN #$ipIdx (leave blank to finish)"
    if ([string]::IsNullOrWhiteSpace($ipInput)) { break }
    $ipEntries += $ipInput.Trim()
    $ipIdx++
}

# Paths
$CSRPath = "$CertFolder\$CertName.csr"
$INFPath = "$CertFolder\$CertName.inf"
$CERPath = "$CertFolder\$CertName.cer"
$PEMPath = "$CertFolder\$CertName.pem"
$PFXPath = "$CertFolder\$CertName.pfx"
$KEYPath = "$CertFolder\$CertName.key"
$RSPPath = "$CertFolder\$CertName.rsp"
$Signature = '$Windows NT$'

# Build INF (dynamic SAN list)
$infContent = @"
[Version]
Signature=$Signature

[NewRequest]
Subject = "$Subject"
KeySpec = 1
KeyLength = 2048
Hashalgorithm = sha256
Exportable = TRUE
FriendlyName = $FriendlyName
MachineKeySet = TRUE
SMIME = False
PrivateKeyArchive = FALSE
UserProtected = FALSE
UseExistingKeySet = FALSE
ProviderName = Microsoft RSA SChannel Cryptographic Provider
ProviderType = 12
RequestType = PKCS10
KeyUsage = 0xa0

[EnhancedKeyUsageExtension]
OID=1.3.6.1.5.5.7.3.1

[Extensions]
2.5.29.17 = "{text}"
"@

foreach ($entry in $dnsEntries) {
    $infContent += "`n_continue_ = `"dns=$entry&`""
}
foreach ($ip in $ipEntries) {
    $infContent += "`n_continue_ = `"ipaddress=$ip&`""
}

$infContent += @"

[RequestAttributes]
CertificateTemplate= WebServerV2
"@

# Write INF
$infContent | Out-File -FilePath $INFPath -Encoding ascii -Force

Write-Host "Creating CertificateRequest (CSR) for $CertName"
if (!(Test-Path $CSRPath)) {
    certreq.exe -new $INFPath $CSRPath | Out-Null
}

Write-Output "Requesting certificate from CA"
certreq.exe -config "mpazica01.root.mpmaterials.com\MPAZICA01" -submit $CSRPath $CERPath $PFXPath | Out-Null
Write-Output "Certificate has been generated."

# Import and repair
Import-Certificate -CertStoreLocation 'Cert:\LocalMachine\My' -FilePath $CERPath | Out-Null
$cert = Get-ChildItem Cert:\LocalMachine\My -Recurse | Where-Object { $_.Subject -like $Subject }
$certThumbprint = $cert.Thumbprint
if ($certThumbprint) { certutil -repairstore my "$certThumbprint" | Out-Null }

# Export PFX (password can be changed as needed)
$pwdtext = "password"
$mypwd   = ConvertTo-SecureString -String $pwdtext -AsPlainText -Force
if ($cert) {
    Export-PfxCertificate -Cert $cert -FilePath $PFXPath -Password $mypwd -Force | Out-Null
}

# Optional split to PEM/KEY
$r = Read-Host "Does this certificate need to be split? (y/n)"
if ($r -eq "y") {
    # Extract private key (temp key.pem)
    openssl pkcs12 -passin pass:$pwdtext -in $PFXPath -nocerts -passout pass:$pwdtext -out key.pem
    # Extract client cert
    openssl pkcs12 -passin pass:$pwdtext -in $PFXPath -clcerts -nokeys -out "$CertName.pem"
    # Remove password from key
    openssl rsa -passin pass:$pwdtext -in key.pem -out "$CertName.key"

    # Extract root (once)
    if (!(Test-Path "$CertFolder\mp-root-ca.pem")) {
        Write-Host "Extracting Root Cert PEM"
        openssl pkcs12 -in $PFXPath -nodes -nokeys -cacerts -out "mp-root-ca.pem"
    }

    Remove-Item "key.pem" -Force

    Write-Host "Review and remove any Bag Attributes/Issuer info if present." -ForegroundColor Yellow
    Start-Process notepad.exe $KEYPath -Wait
    Start-Process notepad.exe $PEMPath -Wait
}

# Cleanup (best effort)
Remove-Item $RSPPath, $INFPath, $CSRPath -ErrorAction SilentlyContinue
if ($certThumbprint) {
    Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Thumbprint -eq $certThumbprint } | Remove-Item -ErrorAction SilentlyContinue
}

Write-Host "Cert processing is complete" -ForegroundColor Cyan
Set-Location C:\Scripts
