import os
import subprocess
import shutil
from pathlib import Path
import argparse, pprint, sys
import time
import tempfile
# for clone_curl()
import requests
import zipfile
import shutil
import os
from io import BytesIO

def run_subprocess(command, check=True,shell=False):
    """
    Wrapper around subprocess.run to capture stdout and stderr.
    
    Args:
        command (list): The command to execute.
        check (bool): Whether to raise an exception on non-zero exit status.

    Returns:
        dict: A dictionary with keys 'stdout' and 'stderr' containing the command's output.
    """
    try:
        # Run the subprocess command with stdout and stderr captured
        result = subprocess.run(
            command,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            check=check,
            shell=shell
        )
        
        # Return the captured stdout and stderr in a dictionary
        return {
            'stdout': result.stdout,
            'stderr': result.stderr,
            'returncode': result.returncode
        }
    except subprocess.CalledProcessError as e:
        # Handle the exception and return the captured output and error
        #print(f"Command '{e.cmd}' failed with return code {e.returncode}")
        return {
            'stdout': e.stdout or '',
            'stderr': e.stderr or '',
            'returncode': 1
        }


class InlineBaseService(): 
    @classmethod
    def get_command_map(cls):
        raise Exception("Unimplemented - please override this method")
        command_map = {
            'example': {
                'required_args': [],
                'method': cls.example,
            },

        }
        return command_map
    
    @classmethod
    def example(cls,**kargs):
        raise Exception("Unimplemented - please ignore this instructive method")

        # Interface for times when persitance allows the command to keep some internal state in self
        import pprint
        print("The Args")
        pprint.pprint(kargs)
        return 1
    
    
    @classmethod
    def run(cls, **kwargs):
        # TO INHERITOR - you may override run for manual control, or leave it in for standad method routing (see examples)
        command_map = cls.get_command_map()

        assert len(kwargs['__command']) == 1, f"Exactly one command must be specified {kwargs['__command']} "
        cmd = kwargs['__command'][0]
        if cmd not in command_map:
            raise ValueError(f"Unknown command: {cmd}")

        command_info = command_map[cmd]
        required_args = command_info.get('required_args', [])
        method = command_info['method']
        for arg in required_args:
            assert arg in kwargs, f"Missing required argument: {arg} for command {cmd}"
        del(kwargs['__command'])
        method_kwargs = kwargs
        return method(**method_kwargs)   
    
    
    @classmethod
    def run_cli(cls):
        """This method parses CLI arguments, converts them to kwargs, and passes them to run."""
        # Create a parser that accepts positional arguments
        parser = argparse.ArgumentParser(description="Generic service CLI for any BaseService subclass.")

        # Add positional argument to capture any argument
        parser.add_argument('args', nargs=argparse.REMAINDER, help='Command arguments and key=value pairs')

        # Parse all the arguments
        args = parser.parse_args()

        # Separate positional arguments (commands) and keyword arguments
        positional_args = []
        kwargs = {}

        for item in args.args:
            if '=' in item:
                # Split key=value pairs
                key, value = item.split('=', 1)
                kwargs[key] = value
            else:
                # Treat anything without '=' as a positional argument
                positional_args.append(item)

        # Store the positional args as a list under the '__command' key
        if positional_args:
            kwargs['__command'] = positional_args

        result = cls.run(**kwargs)
        print(f"{result}")
        return result
        # Suppress stdout and stderr until the result is ready

#
#
#
# -------------------------------------------------------------------------------------
# Stages

import os
import subprocess
import shutil
import time
import traceback
import json

# Assuming run_subprocess is defined elsewhere as per your code

class Stages:

    class Base:
        name = ""
        id = ""
        def verify(settings):
            pass
        def execute(settings):
            pass

        @classmethod
        def get_directory_size(cls,directory):
            """Returns the size of a directory in bytes."""
            if not os.path.exists(directory):
                return 0
            total_size = 0
            for dirpath, dirnames, filenames in os.walk(directory):
                for filename in filenames:
                    file_path = os.path.join(dirpath, filename)
                    if os.path.isfile(file_path):
                        total_size += os.path.getsize(file_path)
            return total_size        

        #
        @classmethod
        def progress(cls,settings):
            """Returns the total size of files in the destination directory in MB."""
            try:
                assert 'target_directory' in settings, "'target_directory' is missing from settings"
                ipfs_dir = settings['target_directory']

                dest_size = cls.get_directory_size(ipfs_dir)
                total_size_mb = dest_size / (1024 * 1024)  # Convert bytes to MB
                return json.dumps({"status": True, "progress": total_size_mb})

            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})
            
    class PythonNative(Base):
        name = "Verify Python Installation"

        @staticmethod
        def verify(settings):
            try:
                subprocess.run(["python3", "--version"], check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
                return json.dumps({"status": True})
            except subprocess.CalledProcessError as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})

        @staticmethod
        def execute(settings):
            try:
                raise RuntimeError("Python installation is not implemented. Please install Python manually.")
            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})


    class DownloadPythonPkg(Base):
        name = "Download Python Pkg"
        id = "download_python_pkg"

        @staticmethod
        def verify(settings):
            try:
                target_path = os.path.join(settings['python_install_dir'], 'v2/bin/python3')
                status = os.path.exists(target_path)
                return json.dumps({"status": status})
            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})

        @staticmethod
        def execute(settings):
            try:
                assert 'python_install_dir' in settings, "'python_install_dir' is missing from settings"
                package_dir = settings['python_install_dir']
                python_dir = os.path.join(package_dir, "v2")
                install_dir = os.path.join(package_dir, "install")

                # Clear the installation directory if it already exists
                if os.path.exists(package_dir):
                    print(f"Clearing the existing installation directory: {package_dir}")
                    shutil.rmtree(package_dir, ignore_errors=True)
                    time.sleep(1)
                    if os.path.exists(package_dir):
                        raise Exception(f"Failed to delete the directory: {package_dir}")
                print(f"Cleaned directory {package_dir}")

                # Recreate the installation directories
                os.makedirs(package_dir)
                os.makedirs(install_dir)

                # Download the Python macOS package
                pkg_path = os.path.join(install_dir, "python-macos.pkg")
                print("Downloading Python for macOS...")
                result = run_subprocess([
                    "curl", "-L", "https://www.python.org/ftp/python/3.9.7/python-3.9.7-macosx10.9.pkg",
                    "-o", pkg_path
                ])
                print("Download stdout:", result['stdout'])
                print("Download stderr:", result['stderr'])

                # Extract the package contents using pkgutil
                expanded_pkg_dir = os.path.join(install_dir, "expanded_pkg")
                print("Expanding the .pkg file...")
                result = run_subprocess([
                    "pkgutil", "--expand", pkg_path, expanded_pkg_dir
                ])
                print("Expand stdout:", result['stdout'])
                print("Expand stderr:", result['stderr'])

                # Process each sub-package
                for sub_pkg in ["Python_Framework.pkg", "Python_Command_Line_Tools.pkg"]:
                    sub_pkg_dir = os.path.join(expanded_pkg_dir, sub_pkg)
                    payload_path = os.path.join(sub_pkg_dir, "Payload")

                    # Check if Payload file exists
                    if not os.path.exists(payload_path):
                        print(f"No Payload found in {sub_pkg_dir}")
                        continue

                    # Extract the Payload using cpio if it's not a tar file
                    extracted_dir = os.path.join(install_dir, f"{sub_pkg}_extracted")
                    os.makedirs(extracted_dir, exist_ok=True)
                    print(f"Extracting {payload_path} using cpio...")
                    result = run_subprocess([
                        "cat", payload_path, "|", "cpio", "-id", "--quiet", "-D", extracted_dir
                    ], shell=True)
                    print("Extraction stdout:", result['stdout'])
                    print("Extraction stderr:", result['stderr'])

                    # Move the extracted files to the v2 directory
                    for item in os.listdir(extracted_dir):
                        item_path = os.path.join(extracted_dir, item)
                        shutil.move(item_path, python_dir)

                # Clean up
                os.remove(pkg_path)
                shutil.rmtree(expanded_pkg_dir)

                # Verify the Python installation
                python_executable = os.path.join(python_dir, 'Python.framework/Versions/3.9/bin/python3')
                print("Running the first Python command...")
                result = run_subprocess([python_executable, "--version"])
                print("Python version stdout:", result['stdout'])
                print("Python version stderr:", result['stderr'])

                return json.dumps({"status": True})
            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})
    
    class DownloadPythonMiniconda(Base):
        name = "Download Python"
        id = "download_python_miniconda"

        @staticmethod
        def verify(settings):
            try:
                if 'python_install_dir' not in settings:
                    return json.dumps({"status": False, "error": "'python_install_dir' is missing from settings"})
                target_path = os.path.join(settings['python_install_dir'], 'v1/bin/python')
                status = os.path.exists(target_path)
                return json.dumps({"status": status})
            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})
        
        @classmethod
        def progress(cls,settings):
            """Returns the total size of files in the destination directory in MB."""
            try:
                assert 'target_directory' in settings, "'target_directory' is missing from settings"
                ipfs_dir = settings['target_directory']
                temp_dir = settings['python_install_dir']
                dest_size = cls.get_directory_size(ipfs_dir)
                dest_size = dest_size+ cls.get_directory_size(temp_dir)

                total_size_mb = dest_size / (1024 * 1024)  # Convert bytes to MB
                return json.dumps({"status": True, "progress": total_size_mb})

            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})
        
        @classmethod
        def execute(cls, settings):
            try:
                assert 'python_install_dir' in settings, "'python_install_dir' is missing from settings"
                
                # Return if system is already present
                verified = json.loads(cls.verify(settings))
                if verified.get("status"):
                    return json.dumps({"status": True})

                # Prepare install directory
                if os.path.exists(settings['python_install_dir']):
                    print(f"Clearing the existing installation directory: {settings['python_install_dir']}")
                    shutil.rmtree(settings['python_install_dir'], ignore_errors=True)
                    time.sleep(5)
                    if os.path.exists(settings['python_install_dir']):
                        raise Exception(f"Failed to delete the directory: {settings['python_install_dir']}")

                # Do the install
                with tempfile.TemporaryDirectory() as temp_package_dir:
                    # settings['python_install_dir']
                    
                    package_dir = temp_package_dir 
                    python_dir = os.path.join(temp_package_dir, "v1")
                    install_dir = os.path.join(temp_package_dir, "install")
                    # Clear the installation directory if it already exists
                    print(f"Cleaned directory {package_dir}")

                    # Recreate the installation directory
                    os.makedirs(package_dir,exist_ok=True) # Support both making, and finding, a target dir
                    os.makedirs(install_dir,exist_ok=True)

                    # Download Miniconda for macOS
                    script_path = os.path.join(install_dir, "Miniconda3-latest-MacOSX-x86_64.sh")
                    print("Downloading Miniconda for macOS...")
                    result = run_subprocess([
                        "curl", "-L", "https://repo.anaconda.com/miniconda/Miniconda3-latest-MacOSX-x86_64.sh",
                        "-o", script_path
                    ])
                    print("Download stdout:", result['stdout'])
                    print("Download stderr:", result['stderr'])
                    # Set execute permissions for the installer
                    os.chmod(script_path, 0o755)

                    # Install Miniconda to the specified directory
                    print("Installing Miniconda...")
                    result = run_subprocess([
                        "bash", script_path, "-b", "-p", python_dir
                    ])
                    print("Install stdout:", result['stdout'])
                    print("Install stderr:", result['stderr'])

                    print("Miniconda installation completed.")

                    # Clean up the installer
                    os.remove(script_path)

                    # Full path to the Miniconda-installed Python
                    shutil.move(package_dir, settings['python_install_dir'])
                python_executable = os.path.join(settings['python_install_dir'],'v1', 'bin/python')
                    
                # Verify the Python installation by running the version command
                print("Running the first Python command...")
                result = run_subprocess([python_executable, "--version"])
                print("Python version stdout:", result['stdout'])
                print("Python version stderr:", result['stderr'])

                return json.dumps({"status": True})
            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})
    

    class SetupSyncthing(Base):
        name = "Setup Syncthing"
        id = "setup_syncthing"

        @staticmethod
        def verify(settings):
            try:
                if 'syncthing_install_dir' not in settings:
                    return json.dumps({"status": False, "error": "'syncthing_install_dir' is missing from settings"})
                target_path = os.path.join(settings['syncthing_install_dir'], 'syncthing')
                status = os.path.exists(target_path)
                return json.dumps({"status": status})
            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})

        @classmethod
        def execute(cls, settings):
            try:
                assert 'syncthing_install_dir' in settings, "'syncthing_install_dir' is missing from settings"

                # Return if Syncthing is already installed
                verified = json.loads(cls.verify(settings))
                if verified.get("status"):
                    return json.dumps({"status": True})

                # Prepare install directory
                if os.path.exists(settings['syncthing_install_dir']):
                    print(f"Clearing the existing Syncthing installation directory: {settings['syncthing_install_dir']}")
                    shutil.rmtree(settings['syncthing_install_dir'], ignore_errors=True)
                    time.sleep(5)
                    if os.path.exists(settings['syncthing_install_dir']):
                        raise Exception(f"Failed to delete the directory: {settings['syncthing_install_dir']}")

                # Create directories for the new installation
                os.makedirs(settings['syncthing_install_dir'], exist_ok=True)

                with tempfile.TemporaryDirectory() as temp_package_dir:
                    # Download Syncthing binary for the appropriate OS (example assumes macOS)
                    syncthing_binary_path = os.path.join(temp_package_dir, "syncthing")
                    print("Downloading Syncthing for macOS...")
                    result = run_subprocess([
                        "curl", "-L", "https://github.com/syncthing/syncthing/releases/latest/download/syncthing-macos-arm64.zip",
                        "-o", os.path.join(temp_package_dir, "syncthing.zip")
                    ])
                    print("Download stdout:", result.stdout)
                    print("Download stderr:", result.stderr)

                    # Unzip the Syncthing binary
                    result = run_subprocess([
                        "unzip", os.path.join(temp_package_dir, "syncthing.zip"), "-d", temp_package_dir
                    ])
                    print("Unzipping Syncthing package completed.")

                    # Move Syncthing binary to the install directory
                    shutil.move(os.path.join(temp_package_dir, "syncthing"), settings['syncthing_install_dir'])
                    os.chmod(syncthing_binary_path, 0o755)  # Make the binary executable

                # Verify Syncthing installation
                syncthing_executable = os.path.join(settings['syncthing_install_dir'], 'syncthing')
                result = run_subprocess([syncthing_executable, "--version"], capture_output=True, text=True)
                print("Syncthing version stdout:", result.stdout)
                print("Syncthing version stderr:", result.stderr)

                # Optionally, start Syncthing and generate default configuration files
                print("Starting Syncthing to generate initial configuration...")
                result = run_subprocess([syncthing_executable, "-generate", settings['syncthing_install_dir']], capture_output=True, text=True)
                print("Syncthing initial configuration completed.")

                return json.dumps({"status": True})
            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})


    class SetupVirtualEnv(Base):
        name = "Setup Virtual Environment"
        id = "setup_virtual_env"
        __requiredModules = ['requests']

        @staticmethod
        def get_venv_paths(target_dir):
            if os.name == 'nt':  # Windows
                venv_python = os.path.join(target_dir, 'Scripts', 'python.exe')
                venv_pip = os.path.join(target_dir, 'Scripts', 'pip.exe')
            else:  # Unix/Linux/Mac
                venv_python = os.path.join(target_dir, 'bin', 'python')
                venv_pip = os.path.join(target_dir, 'bin', 'pip')
            return venv_python, venv_pip

        @classmethod
        def verify(cls,settings):
            try:
                # Ensure required settings are present
                assert 'target_directory' in settings, "'target_directory' is missing from settings"
                settings['required_modules' ] = cls.__requiredModules
                target_dir = settings['target_directory']
                required_modules = settings['required_modules']

                # Get the path to the virtual environment's python
                venv_python, _ = cls.get_venv_paths(target_dir)

                if not os.path.exists(venv_python):
                    return json.dumps({"status": False, "error": "Virtual environment not found in target_directory"})

                # For each required module, check if it is installed
                missing_modules = []
                for module in required_modules:
                    cmd = [venv_python, '-c', f'import {module}']
                    result = run_subprocess(cmd)
                    if result['returncode'] != 0:
                        missing_modules.append(module)

                if missing_modules:
                    error_msg = f"The following required modules are missing in the virtual environment: {', '.join(missing_modules)}"
                    return json.dumps({"status": False, "error": error_msg})
                else:
                    return json.dumps({"status": True})

            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})

        @classmethod
        def execute(cls, settings):
            try:
                # First, attempt to verify
                verified = json.loads(cls.verify(settings))
                if verified.get("status"):
                    print("Virtual environment is already set up and all required modules are installed.")
                    return json.dumps({"status": True})

                # Ensure required settings are present
                assert 'target_directory' in settings, "'target_directory' is missing from settings"
                assert 'target_python3_file' in settings, "'target_python3_file' is missing from settings"
                settings['required_modules'] = cls.__requiredModules
                target_dir = settings['target_directory']
                required_modules = settings['required_modules']
                python_executable = settings['target_python3_file']

                # Create the virtual environment
                print("Creating virtual environment...")
                cmd = [python_executable, '-m', 'venv', target_dir]
                result = run_subprocess(cmd)
                if result['returncode'] != 0:
                    error_msg = f"Failed to create virtual environment: {result['stderr']}"
                    return json.dumps({"status": False, "error": error_msg})

                # Get paths to virtual environment's python and pip
                venv_python, venv_pip = cls.get_venv_paths(target_dir)

                if not os.path.exists(venv_pip):
                    return json.dumps({"status": False, "error": "pip not found in virtual environment"})

                # Install required modules using pip in the virtual environment
                for module in required_modules:
                    print(f"Installing {module}...")
                    cmd = [venv_pip, 'install', module]
                    result = run_subprocess(cmd)
                    if result['returncode'] != 0:
                        error_msg = f"Failed to install module {module}: {result['stderr']}"
                        return json.dumps({"status": False, "error": error_msg})

                # Verify again after installation
                verified = json.loads(cls.verify(settings))
                if verified.get("status"):
                    return json.dumps({"status": True})
                else:
                    return json.dumps({"status": False, "error": "Verification failed after installation"})

            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})

    class DownloadIPFS(Base):
        name = "Download IPFS"
        id = "download_ipfs"

        @staticmethod
        def verify(settings):
            # ipfs_install_dir
            try:
                assert 'target_directory' in settings, "'target_directory' is missing from settings"
                ipfs_executable = os.path.join(settings['target_directory'], 'ipfs')

                if not os.path.exists(ipfs_executable):
                    return json.dumps({"status": False,"error":"No ipfs_executable present"})

                #result = subprocess.run(
                #    [ipfs_executable, "--version"],
                #    stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, check=True
                #)
                result = run_subprocess([ipfs_executable, "--version"])                
                
                # Assuming IPFS version output should contain "ipfs version"
                if "ipfs version" in result['stdout']:
                    return json.dumps({"status": True})
                else:
                    return json.dumps({"status": False,"error":"Could not read IPFS version from CLI"})
            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})

        @classmethod
        def execute(cls, settings):
            try:
                verified = json.loads(cls.verify(settings))
                if verified.get("status"):
                    print("IPFS is already installed and verified.")
                    return json.dumps({"status": True})

                assert 'target_directory' in settings, "'target_directory' is missing from settings"
                ipfs_dir = settings['target_directory']
                ipfs_archive = os.path.join(ipfs_dir, 'go-ipfs.tar.gz')

                # Create the IPFS installation directory if it does not exist
                os.makedirs(ipfs_dir, exist_ok=True)


                print("Downloading IPFS for macOS...")
                print("-----------------------------")
                result =  run_subprocess([
                    "curl", "-L", "https://dist.ipfs.io/go-ipfs/v0.11.0/go-ipfs_v0.11.0_darwin-amd64.tar.gz",
                    "-o", ipfs_archive
                ])   
                print(result['stdout'])
                print(result['stderr'])

                print("Extracting IPFS...")
                print("-----------------------------")
                result =  run_subprocess(["tar", "-xvzf", ipfs_archive, "-C", ipfs_dir, "--strip-components=1"])
                print(result['stdout'])
                print(result['stderr'])
                print("IPFS download and extraction completed.")

                # Clean up the archive
                os.remove(ipfs_archive)

                return json.dumps({"status": True})
            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})







    class DownloadRepo(Base):
        name = "Download Repository"
        id = "download_repo"

        @staticmethod
        def execute(settings):
            try:
                assert 'target_repo' in settings, "'target_repo' is missing from settings"
                assert 'target_directory' in settings, "'target_directory' is missing from settings"
                assert 'target_branch' in settings, "'target_branch' is missing from settings"
                repo_url = settings['target_repo']
                target_path = settings['target_directory']
                repo_name = repo_url.split("/")[-1]  # Extracts the repo name from the URL
                zip_url = f"{repo_url}/archive/refs/heads/{settings['target_branch']}.zip"

                # Add Authorization header if access_token is provided (for private repos)
                headers = {}
                if 'access_token' in settings:
                    headers['Authorization'] = f"token {settings['access_token']}"

                # Download the ZIP file
                try:
                    print(f"Downloading repository from {zip_url}")
                    response = requests.get(zip_url, headers=headers, stream=True)
                    response.raise_for_status()

                    # Extract ZIP file contents to the target path
                    with zipfile.ZipFile(BytesIO(response.content)) as zip_ref:
                        zip_ref.extractall(target_path)
                    # Move the contents of the subdirectory to the target path
                    top_level_dir = next(os.scandir(target_path)).path  # Get the path of the top-level extracted directory
                    for item in os.listdir(top_level_dir):
                        shutil.move(os.path.join(top_level_dir, item), target_path)                
                    shutil.rmtree(top_level_dir)
                    os.makedirs(os.path.join(target_path,'.git'), exist_ok=True)
                    
                    print(f"Repository downloaded and extracted to: {target_path}")
                except requests.exceptions.RequestException as e:
                    print(f"Error downloading repository: {str(e)}")
                    raise RuntimeError(f"Failed to download the repository: {str(e)}")
                except zipfile.BadZipFile as e:
                    print(f"Error extracting ZIP file: {str(e)}")
                    raise RuntimeError(f"Failed to extract ZIP file: {str(e)}")     
            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info})
                

        @staticmethod
        def verify(settings):
            try:
                assert 'target_directory' in settings, "'target_directory' is missing from settings"
                repodir_exists = os.path.exists(settings['target_directory'])

                
                # Assuming IPFS version output should contain "ipfs version"
                if repodir_exists:
                    return json.dumps({"status": True})
                else:
                    return json.dumps({"status": False,"error":"Find Repo Dir"})
            except Exception as e:
                error_info = f"{str(e)}\n{traceback.format_exc()}"
                return json.dumps({"status": False, "error": error_info}) 
