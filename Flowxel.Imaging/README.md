# Install procedure of openCV on Arch

install opencv from pacman

```shell
$ pacman -S opencv4
```

add nuget packages

```xaml
        <PackageReference Include="OpenCvSharp4" Version="4.11.0.20250507" />
        <PackageReference Include="OpenCvSharp4.Extensions" Version="4.11.0.20250507" />
```

install opencvsharp extern

```shell
$ git clone https://github.com/shimat/opencvsharp.git
$ cd opencvsharp
$ mkdir build
$ cmake -S ../src \
  -D CMAKE_INSTALL_PREFIX=/usr \
  -DCMAKE_POLICY_VERSION_MINIMUM=3.5
$ cmake --build . --parallel "$(nproc)"
$ sudo cp libOpenCvSharpExtern.so /usr/lib/
```