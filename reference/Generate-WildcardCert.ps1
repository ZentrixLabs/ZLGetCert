# Create a wildcard certificate request for *.root.mpmaterials.com using internal Windows CA
# Requires -RunAsAdministrator

# Set organizational details
$Location = Read-Host "Enter City"
$State = Read-Host "Enter State Abbreviation"
$OU = "IT"
$Company = "root.mpmaterials.com"

# Define certificate properties
$CertName = "*.root.mpmaterials.com"
$FileSafeCertName = "_root.mpmaterials.com" # Replace * with _ for file names
$FriendlyName = "wildcard_root_mpmaterials"
$Subject = "CN=$CertName,OU=$OU,O=$Company,L=$Location,S=$State,C=US"

# Create C:\ssl folder if it doesn't exist
$CertFolder = "C:\ssl"
if (!(Test-Path $CertFolder)) {
    New-Item -Path $CertFolder -Type Directory
}

Set-Location "C:\ssl"

# Define file paths with file-safe name
$CSRPath = "$CertFolder\$($FileSafeCertName).csr"
$INFPath = "$CertFolder\$($FileSafeCertName).inf"
$CERPath = "$CertFolder\$($FileSafeCertName).cer"
$PEMPath = "$CertFolder\$($FileSafeCertName).pem"
$PFXPath = "$CertFolder\$($FileSafeCertName).pfx"
$KEYPath = "$CertFolder\$($FileSafeCertName).key"
$RSPPath = "$CertFolder\$($FileSafeCertName).rsp"
$Signature = '$Windows NT$'

Write-Host "Creating CertificateRequest(CSR) for $CertName"

# Create INF file for wildcard certificate
$INF = @"
[Version]
Signature=$Signature

[NewRequest]
Subject = "$Subject"
KeySpec = 1
KeyLength = 2048
HashAlgorithm = sha256
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
_continue_ = "dns=$CertName"

[RequestAttributes]
CertificateTemplate=WebServerV2
"@

# Generate CSR
if (!(Test-Path $CSRPath)) {
    Write-Host "Certificate Request is being generated"
    $INF | Out-File -FilePath $INFPath -Force
    certreq.exe -new $INFPath $CSRPath
}

Write-Output "Certificate Request has been generated"

# Submit CSR to CA and generate certificate
Write-Output "Requesting certificate from CA"
$cername = $FileSafeCertName + ".cer"
$pfx = $FileSafeCertName + ".pfx"
certreq.exe -config mpazica01.root.mpmaterials.com\MPAZICA01 -submit $CSRPath $CERPath $pfx

Write-Output "Certificate has been generated."

# Import certificate to local machine store
Write-Output "Importing certificate to local machine store"
Import-Certificate -CertStoreLocation 'Cert:\LocalMachine\My' -FilePath $CERPath

# Wait briefly to ensure store is updated
Start-Sleep -Seconds 2

# Retrieve certificate with exact subject match
Write-Output "Retrieving certificate from store"
$cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.FriendlyName -eq $FriendlyName }

if ($null -eq $cert) {
    Write-Error "Failed to find certificate with Subject: $Subject and FriendlyName: $FriendlyName in Cert:\LocalMachine\My"
    exit 1
}

Write-Output "Found certificate: Thumbprint=$($cert.Thumbprint), Subject=$($cert.Subject)"

# Repair certificate store to associate private key
$certThumbprint = $cert.Thumbprint
certutil -repairstore my "$certThumbprint"

# Export PFX with password
Write-Output "Exporting PFX certificate"
$pwdtext = "password"
$mypwd = ConvertTo-SecureString -String $pwdtext -Force -AsPlainText
$cert | Export-PfxCertificate -FilePath $PFXPath -Password $mypwd -Force

# Prompt to split certificate for HAProxy
$r = Read-Host "Does this certificate need to be split for HAProxy? (y/n)"
if ($r -eq "y") {
    # Extract private key
    openssl pkcs12 -passin pass:$pwdtext -in $PFXPath -nocerts -passout pass:$pwdtext -out key.pem

    # Extract client certificate
    openssl pkcs12 -passin pass:$pwdtext -in $PFXPath -clcerts -nokeys -out "$FileSafeCertName.pem"

    # Remove password from private key
    openssl rsa -passin pass:$pwdtext -in key.pem -out "$FileSafeCertName.key"

    # Extract root CA certificate
    if (!(Test-Path "C:\ssl\mp-root-ca.pem")) {
        Write-Host "Extracting Root CA PEM"
        openssl pkcs12 -in $PFXPath -nodes -nokeys -cacerts -out "C:\ssl\mp-root-ca.pem"
    }

    # Cleanup temporary key file
    Remove-Item "key.pem" -Force

    Write-Host "Check all created files and remove Bag Attributes and Issuer Information from the files." -ForegroundColor Yellow
    Start-Process notepad.exe $KEYPath -Wait
    Start-Process notepad.exe $PEMPath -Wait
}

# Cleanup
Write-Output "Cleaning up temporary files"
Remove-Item $RSPPath -ErrorAction SilentlyContinue
Remove-Item $INFPath -ErrorAction SilentlyContinue
Remove-Item $CSRPath -ErrorAction SilentlyContinue
Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Thumbprint -eq $certThumbprint } | Remove-Item

Write-Host "Wildcard certificate processing is complete" -ForegroundColor Cyan

Set-Location C:\Scripts