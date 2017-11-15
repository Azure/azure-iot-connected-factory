::# Sample shell script to start the publisher components on a gateway device.
::# This script assumes, that it runs on a Linux system

::# Please set the following variabels to your local requierements:

::# Define your IoT-hub connect string
set _HUB_CS='replace with your connect string'
::#Set the docker shared root variable, so that the docker containers could access the files stored on your real drive:
::# In the example below, it is assumed, that drive D: is a shared driver for docker, and it contains the directory docker.
set DOCKER_SHARED_ROOT=//C/docker

::# Insert the hostname with domain of the publisher and IP address to these variables:
set MYHOSTNAME=publisher.example.com
set MYIP=10.123.45.26

docker network create -d bridge iot_edge
::# Initially run the publisher to register itself at the IoT-hub:

docker run -it --rm --name proxy --network iot_edge -v %DOCKER_SHARED_ROOT%:/mapped microsoft/iot-gateway-opc-ua-proxy:1.0.2 -i -c "%_HUB_CS%" -D /mapped/cs.db
::# Run the proxy permanently, so that the connected factory could connect to OPC UA servers through the proxy tunnel:
::# Workaround for https://github.com/Azure/iot-edge-opc-proxy/issues/79 :
::# Use docker option --restart always
docker run -it --restart always --name proxy --network iot_edge -v %DOCKER_SHARED_ROOT%:/mapped --add-host "%MYHOSTNAME%":%MYIP% microsoft/iot-gateway-opc-ua-proxy:1.0.2 -D /mapped/cs.db

:foreverloop
	::# Get output of the restarted docker container proxy:
	docker container attach proxy
goto foreverloop
