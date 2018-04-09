# AWSHelper
Amazon Web Service Helper - CLI Tool To Manage AWS To Aid Where Terraform Lacks

Set variable or Enviroment Variable of $AWSHelper into: /drive/some/path/AWSHelper.dll 

Then you can execute in bash shell with single command:

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
