::# Sample shell script to start the publisher components on a gateway device.
::# This script assumes, that it runs on a Linux system

::# Please set the following variabels to your local requierements:

::# Define your IoT-hub connect string
set _HUB_CS='replace with your connect string'
::#Set the docker shared root variable, so that the docker containers could access the files stored on your real drive:
::# In the example below, it is assumed, that drive D: is a shared driver for docker, and it contains the directory docker.
set DOCKER_SHARED_ROOT=//C/docker

docker network create -d bridge iot_edge
::# Run the publisher permanently, so that it is accessible by port 62222 from the "outside"
docker run -it --rm --network iot_edge -h publisher --name publisher -p 62222:62222 -v %DOCKER_SHARED_ROOT%:/docker -v myx509certstore:/root/.dotnet/corefx/cryptography/x509stores microsoft/iot-edge-opc-publisher:2.1.2 publisher.example.com "%_HUB_CS%" --lf ./publisher.log.txt --ns true --pf /docker/publishednodes.json --ih Http1 --as true --tm true --si 0 --ms 0 --sd "example.com" --trustedcertstorepath=/docker/CertificateStores/trusted --rejectedcertstorepath=/docker/CertificateStores/rejected
