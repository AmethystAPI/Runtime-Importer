set_languages("c++23")
set_project("symgen")
set_version(string.format("%d.%d.%d", 0, 0, 1))
set_symbols("debug")
set_targetdir("build")

target("symgen")
    set_kind("binary")

    add_files(
        "src/**.cpp",
        "include/**.cpp",
        "include/**.c"
    )

    add_includedirs(
        "include"
    )

    local clang_args = {
        "-x", "c++",
        "-std=c++23",
        "-IC:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.44.35207/include",
        "-IC:/Program Files (x86)/Windows Kits/10/Include/10.0.26100.0/ucrt",
        "-IC:/Program Files (x86)/Windows Kits/10/Include/10.0.26100.0/shared",
        "-IC:/Program Files (x86)/Windows Kits/10/Include/10.0.26100.0/um",
    
        "-I$(projectdir)/test/headers/src",
        "-I$(projectdir)/test/headers/include",
        "-fms-extensions",
        "-fms-compatibility"
    }

    add_links("$(projectdir)/lib/libclang.lib")
    add_includedirs("src", {public = true})
    add_headerfiles("src/**.hpp")
    local runargs = {"--input-directory", "$(projectdir)/test/headers/src", "--generated-directory", "$(projectdir)/generated/", "--filters", "minecraft"}
    for _, arg in ipairs(clang_args) do
        table.insert(runargs, arg)
    end
    set_runargs(runargs)
    set_rundir("$(projectdir)/build")