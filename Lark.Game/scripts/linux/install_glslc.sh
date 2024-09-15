#!/bin/zsh

# Check if glslc is already installed
if ! command -v glslc &> /dev/null; then
  # Install glslc
  echo "Installing glslc..."
  mkdir -p /mnt/d/config/shaderrc
  cd /mnt/d/config/shaderrc

  # If no install.tgz file is found, download it

  if [ -f install.tgz ]; then
    echo "install.tgz found."
  else
    echo "install.tgz not found. Downloading..."
    wget https://storage.googleapis.com/shaderc/artifacts/prod/graphics_shader_compiler/shaderc/linux/continuous_gcc_release/450/20240308-114748/install.tgz
  fi

  # downloads an install.tgx file. Extract it.
  # if not extracted, extract it
  if [ -d install ]; then
    echo "install directory found."
  else
    echo "install directory not found. Extracting..."
    tar -xvf install.tgz
  fi

  # copy the results from install/bin to /mnt/d/config/shaderrc
  cp -r install/bin/* /mnt/d/config/shaderrc

  # Add the extracted folder to PATH
  echo "glslc installed successfully!"
  
  # Add /mnt/d/config/shaderrc to PATH
  echo 'export PATH="/mnt/d/config/shaderrc:$PATH"' >> ~/.zshrc
  source ~/.zshrc
else
  echo "glslc is already installed."
fi