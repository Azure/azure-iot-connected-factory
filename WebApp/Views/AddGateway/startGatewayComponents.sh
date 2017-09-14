#!/bin/bash
# Sample shell script to start the requiered components on a gateway device.
# This script assumes, that it runs on a Linux system

# Please set the following variabels to your local requierements:

# Define your IoT-hub connect string
#(within the '' to prevent the shell from doing bad things with the special characters like the = ):
_HUB_CS='replace with your connect string'

# Set the docker shared root variable, so that the docker containers could access the files stored on your real drive:
# In the example below, it is assumed, that on Linux the directory /shared is shared with the docker containers.
DOCKER_SHARED_ROOT=/shared
# Insert your hostname and IP address to these variables:
set MYHOSTNAME=`hostname`
set MYIP=192.168.237.10
# Initially run the publisher to register itself at the IoT-hub:

# Initialy run the publisher to initialize itself and register itself at the IoT-hub:
docker run -it --rm -h publisher -v ${DOCKER_SHARED_ROOT}:/build/out/CertificateStores -v ${DOCKER_SHARED_ROOT}:/root/.dotnet/corefx/cryptography/x509stores microsoft/iot-edge-opc-publisher:2.0.3 publisher "$_HUB_CS"
# Run the publisher permanently, so that it is accessible by port 62222 from the "outside"
docker run -it --rm -h publisher --expose 62222 -p 62222:62222 -v ${DOCKER_SHARED_ROOT}:/build/out/Logs -v ${DOCKER_SHARED_ROOT}:/build/out/CertificateStores -v ${DOCKER_SHARED_ROOT}:/shared -v ${DOCKER_SHARED_ROOT}:/root/.dotnet/corefx/cryptography/x509stores -e _GW_PNFP="/shared/publishednodes.JSON" microsoft/iot-edge-opc-publisher:2.0.3 publisher &

# Initially run the publisher to register itself at the IoT-hub:
docker run -it --rm -v ${DOCKER_SHARED_ROOT}:/mapped microsoft/iot-edge-opc-proxy:1.0.3 -i -c "$_HUB_CS" -D /mapped/cs.db
# Run the proxy permanently, so that the connected factory could connect to OPC UA servers through the proxy tunnel:
docker run -it --rm -v ${DOCKER_SHARED_ROOT}:/mapped --add-host "$MYHOSTNAME publisher":$MYIP microsoft/iot-edge-opc-proxy:1.0.3 -D /mapped/cs.db &
