##Generate cert using provided CSR
$csrpath = Read-host "Enter path to CSR"
certreq.exe -submit -attrib "CertificateTemplate:WebServerV2" $csrpath