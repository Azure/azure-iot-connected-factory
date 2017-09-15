::# Sample shell script to start the publisher components on a gateway device.
::# This script assumes, that it runs on a Linux system

::# Please set the following variabels to your local requierements:

::# Define your IoT-hub connect string
set _HUB_CS='replace with your connect string'
::#Set the docker shared root variable, so that the docker containers could access the files stored on your real drive:
::# In the example below, it is assumed, that drive D: is a shared driver for docker, and it contains the directory docker.
set DOCKER_SHARED_ROOT=//D/docker

::# Insert your hostname and IP address to these variables:
set MYHOSTNAME=DESKTOP-0EBERG1.example.net
set MYIP=192.168.237.10
::# Initially run the publisher to register itself at the IoT-hub:
docker run -it --rm -v %DOCKER_SHARED_ROOT%:/mapped microsoft/iot-edge-opc-proxy:1.0.3 -i -c "%_HUB_CS%" -D /mapped/cs.db
::# Run the proxy permanently, so that the connected factory could connect to OPC UA servers through the proxy tunnel:
docker run -it --rm -v %DOCKER_SHARED_ROOT%:/mapped --add-host "%MYHOSTNAME%":%MYIP% microsoft/iot-edge-opc-proxy:1.0.3 -D /mapped/cs.db
