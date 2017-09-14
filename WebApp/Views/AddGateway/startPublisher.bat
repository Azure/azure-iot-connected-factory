::# Sample shell script to start the publisher components on a gateway device.
::# This script assumes, that it runs on a Linux system

::# Please set the following variabels to your local requierements:

::# Define your IoT-hub connect string
set _HUB_CS='replace with your connect string'
::#Set the docker shared root variable, so that the docker containers could access the files stored on your real drive:
::# In the example below, it is assumed, that drive D: is a shared driver for docker, and it contains the directory docker.
set DOCKER_SHARED_ROOT=//D/docker

::# Initialy run the publisher to initialize itself and register itself at the IoT-hub:
docker run -it --rm -h publisher -v %DOCKER_SHARED_ROOT%:/build/out/CertificateStores -v %DOCKER_SHARED_ROOT%:/root/.dotnet/corefx/cryptography/x509stores microsoft/iot-edge-opc-publisher:2.0.3 publisher "%_HUB_CS%"
::# Run the publisher permanently, so that it is accessible by port 62222 from the "outside"
docker run -it --rm -h publisher --expose 62222 -p 62222:62222 -v %DOCKER_SHARED_ROOT%:/build/out/Logs -v %DOCKER_SHARED_ROOT%:/build/out/CertificateStores -v %DOCKER_SHARED_ROOT%:/shared -v %DOCKER_SHARED_ROOT%:/root/.dotnet/corefx/cryptography/x509stores -e _GW_PNFP="/shared/publishednodes.JSON" microsoft/iot-edge-opc-publisher:2.0.3 publisher
