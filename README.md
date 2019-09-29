# AWSHelper
Amazon Web Service Helper - CLI Tool To Manage AWS To Aid Where Terraform Lacks

Set variable or Enviroment Variable of $AWSHelper into: /drive/some/path/AWSHelper.dll 

Then you can execute in bash shell with single command:

Displaying Help Info:
dotnet $AWSHelper help

Examples:

Deleting Service:
dotnet $AWSHelper ecs destroy-service --service="service-name"

Deleting Task Definition:
dotnet $AWSHelper ecs destroy-task-definitions --family="task-definition-family"

Deleting Load Balancer:
dotnet $AWSHelper elb destroy-load-balancer --name="load-balancer-name"

Deleting Log Group:
dotnet $AWSHelper cloud-watch destroy-log-group --name="log-group-name"

Awaiting for Desire Count of Service to reach Running count level:
dotnet $AWSHelper ecs await-service-start  --service="service-name" --timeout=120

Deleting Route53 Records
dotnet $AWSHelper route53 destroy-record --name="www.subnet.domain." --type="CNAME" --zone="zone_id"


## Hash Store

### Download
```
AWSHelper s3 hash-download --id="job-identifier" \
 --source="bucket_name/hash-files" \
 --status="bucket_name/status" \
 --destination="tmp/local-output-dir" \
 --sync="tmp/download-status-dir" \
 --wipe=true 
```

### Upload

> retention - time in seconds after which status file becomes obsolete
> rotation - maximum number of status files to be kept

```
AWSHelper s3 hash-download --id="job-identifier" \
 --destination="bucket_name/hash-files" \
 --status="bucket_name/status" \
 --source="tmp/local-output-dir" \
 --sync="tmp/upload-status-dir" \
 --recursive=true \
 --rotation=31 \
 --retention=1 \ 
 --wipe=true 
```
