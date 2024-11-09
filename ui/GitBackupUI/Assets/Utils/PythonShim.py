import os
import json
import sys
import io

def join_path(*args):
    """Simple path join replacement."""
    return "/".join(args)

def run_subprocess(command):
    """Minimal subprocess.run() replacement for MicroPython."""
    temp_file = "/tmp/micropython_output.txt"
    command_str = " ".join(command)+"" + f" > {temp_file} 2>&1"
    return_code = os.system(command_str)

    output = ""
    if "/tmp/micropython_output.txt" in temp_file:
        try:
            with open(temp_file, "r") as file:
                output = file.read()
            os.remove(temp_file)
        except OSError:
            pass

    if return_code != 0:
        raise RuntimeError(f"Command '{command_str}' failed with exit code {return_code}")

    return {"returncode": return_code, "stdout": output, "stderr": ""}

def verify(target_directory):
    try:
        python_path = join_path(target_directory, "v1/bin/python")
        status = os.stat(python_path)  # Checks if path exists (similar to os.path.exists)
        return json.dumps({"status": bool(status)})
    except OSError:
        return json.dumps({"status": False, "error": "Python not found at the expected path"})

'''
def clear_directory(target_directory):
    try:
        # List all items in the target directory
        for item in os.listdir(target_directory):
            item_path = join_path(target_directory, item)
            
            # Check if it's a file or directory
            if os.stat(item_path)[0] & 0x4000:  # Check if it's a directory
                clear_directory(item_path)  # Recursively clear the directory
                os.rmdir(item_path)  # Remove the empty directory
            else:
                os.remove(item_path)  # Remove the file

        # Remove the target directory itself if needed
        os.rmdir(target_directory)
    except OSError:
        pass  # Handle any errors silently if the directory or files do not exist
'''

def clear_directory_ai(target_directory):
    # Check if the target directory exists
    try:
        items = os.listdir(target_directory)
    except OSError as e:
        if e.args[0] == 2:  # Error code 2: No such file or directory
            return  # Exit if the directory does not exist
        else:
            raise RuntimeError(f"Failed to list directory '{target_directory}': {e}")

    for item in items:
        item_path = target_directory + "/" + item  # Construct the path manually
        
        try:
            # Check if it's a directory
            if os.stat(item_path)[0] & 0x4000:  # Directory flag in MicroPython
                clear_directory(item_path)  # Recursively clear subdirectories
                try:
                    os.rmdir(item_path)  # Attempt to remove the empty directory
                except OSError as e:
                    if e.args[0] != 2:  # Raise if error is not 'No such file or directory'
                        raise RuntimeError(f"Failed to delete directory '{item_path}': {e}")
            else:
                try:
                    os.remove(item_path)  # Attempt to remove the file
                except OSError as e:
                    if e.args[0] != 2:  # Raise if error is not 'No such file or directory'
                        raise RuntimeError(f"Failed to delete file '{item_path}': {e}")

        except OSError as e:
            # Raise any stat-related errors other than 'No such file or directory'
            if e.args[0] != 2:
                raise RuntimeError(f"Failed to access '{item_path}': {e}")

    # Attempt to remove the target directory itself to ensure it's fully cleaned
    try:
        os.rmdir(target_directory)
    except OSError as e:
        # Ignore 'No such file or directory' error for the target directory itself
        if e.args[0] != 2:
            raise RuntimeError(f"Failed to remove target directory '{target_directory}': {e}")
import os
import time

def clear_directory(target_directory):
    # Use a shell command to delete the directory and its contents recursively
    result = os.system(f"rm -rf {target_directory}")
    
    if result != 0:
        raise RuntimeError(f"Failed to delete directory tree '{target_directory}' with rm -rf command")

    # Check if the directory still exists
    for _ in range(3):  # Retry up to 3 times, if necessary
        try:
            os.stat(target_directory)  # Check if directory exists
            time.sleep(0.1)  # Short wait before retrying
        except OSError as e:
            if e.args[0] == 2:  # Error code 2: No such file or directory
                return  # Directory successfully deleted
            else:
                raise RuntimeError(f"Error checking directory '{target_directory}': {e}")

    # If directory still exists after retries, raise an error
    raise RuntimeError(f"Directory '{target_directory}' still exists after attempting deletion")



def makedirs(path, exist_ok=True):
    """
    Recursively create directories for the given path, starting from the parent directory if needed.
    
    :param path: Path to create.
    :param exist_ok: If True, ignore the error if the directory already exists.
    """
    try:
        os.mkdir(path)  # Try to make the directory directly
    except OSError as e:
        if e.args[0] == 17 and exist_ok:  # Directory already exists
            return
        elif e.args[0] == 2:  # Parent directory does not exist
            # Recursively create the parent directory by peeling off the last component
            parent_dir = path.rsplit("/", 1)[0]  # Get the parent directory
            if parent_dir:  # Ensure we don't go to an empty string on root
                makedirs(parent_dir, exist_ok=exist_ok)
            # Try creating the directory again
            try:
                os.mkdir(path)
            except:
                pass
        else:
            raise




def execute(target_directory):
    try:
        if json.loads(verify(target_directory)).get("status"):
            return json.dumps({"status": True})
        # Clear target directory if it exists
        temp_dir = "/tmp/micropython_install"
        package_dir = join_path(temp_dir, "v1")
        install_dir = join_path(temp_dir, "install")

        #
        # Do Install
        
        clear_directory(temp_dir)
        # return json.dumps({"status": False})
        # Temporary directory for download and install
        makedirs(temp_dir)
        makedirs(target_directory)
        makedirs(target_directory, exist_ok=True)
        makedirs(temp_dir, exist_ok=True)
        #makedirs(package_dir, exist_ok=True)
        makedirs(install_dir, exist_ok=True)
         
        # Download Miniconda installer
        script_path = join_path(install_dir, "Miniconda3-latest-MacOSX-x86_64.sh")
        run_subprocess([
            "curl", "-L", "https://repo.anaconda.com/miniconda/Miniconda3-latest-MacOSX-x86_64.sh",
            "-o", script_path
        ])
        os.system(f"chmod 755 {script_path}")

        # Install Miniconda
        command = ["bash", script_path, "-b", "-p", package_dir]
        print("Running ", ' '.join(command))
        run_subprocess(command)
        
        print("Installing")
        #
        # Do Movement
        clear_directory(target_directory)
        makedirs(target_directory, exist_ok=True)
        os.system(f"cp -R '{package_dir}' '{target_directory}'")
        print(f"Doing cp -R '{package_dir}' '{target_directory}")
        # Verify by running the installed Python version command
        python_executable = join_path(target_directory, "v1/bin/python")
        result = run_subprocess(['"'+python_executable+'"', "--version"])
        print("Python version:", result["stdout"])
        print("Python version:", result["stderr"])
        clear_directory(temp_dir)

        return json.dumps({"status": True})

    except Exception as e:
        error_output = io.StringIO()
        sys.print_exception(e, error_output)  # Capture the traceback into a string
        error_trace = error_output.getvalue()
        return json.dumps({"status": False, "error":error_trace})


if __name__ == "__main__":
    try:
        # Ensure we have at least 4 arguments
        if len(sys.argv) < 4:
            raise ValueError("Usage: PythonShim.py stage id=<stage_id> mode=<verify|execute> target_directory=<path>")

        # Parse arguments
        stage = sys.argv[1]
        id_arg = sys.argv[2]
        mode_arg = sys.argv[3]
        target_dir_arg = sys.argv[4] if len(sys.argv) > 4 and sys.argv[4].startswith("target_directory=") else None

        # Extract id, mode, and target_directory values
        if not id_arg.startswith("id=") or not mode_arg.startswith("mode="):
            raise ValueError("Invalid argument format. Expected id=<stage_id> and mode=<verify|execute>.")

        stage_id = id_arg.split("=", 1)[1]
        mode = mode_arg.split("=", 1)[1]
        target_directory = target_dir_arg.split("=", 1)[1] if target_dir_arg else "./install_dir"  # Default path if not specified

        # Run the appropriate function based on mode
        if mode == "verify":
            result = verify(target_directory)
        elif mode == "execute":
            result = execute(target_directory)
        else:
            raise ValueError(f"Unknown mode: {mode}")

        # Output the result in JSON format
        print(result)

    except Exception as e:
        # Return error message in JSON format
        error_info = {"status": False, "error": str(e)}
        print(json.dumps(error_info))
