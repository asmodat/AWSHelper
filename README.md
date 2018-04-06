# AWSHelper
Amazon Web Service Helper - CLI Tool To Manage AWS To Aid Where Terraform Lacks


Sample Use Cases:

Deleting Service:
dotnet AWSHelper.dll ecs destroy-service --service="<some-service-name>"

Deleting Task Definition:
dotnet AWSHelper.dll ecs destroy-task-definitions --family="<task-definition-family>"

Deleting Load Balancer:
dotnet AWSHelper.dll elb destroy-load-balancer --name="<load-balancer-name>"

Deleting Log Group:
dotnet AWSHelper.dll cloud-watch destroy-log-group --name="<log-group-name>"
