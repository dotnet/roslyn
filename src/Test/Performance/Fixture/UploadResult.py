import os
import sys
import helix.azure_storage

uploadClient = helix.azure_storage.BlobUploadClient(os.environ["HELIX_RESULTS_CONTAINER_URI"], os.environ["HELIX_RESULTS_CONTAINER_WSAS"], os.environ["HELIX_RESULTS_CONTAINER_RSAS"])
url = uploadClient.upload(sys.argv[1], sys.argv[2])
