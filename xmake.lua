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
        "-std=c++20",
        "-IC:/Program Files (x86)/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.44.35207/include",
        "-IC:/Program Files (x86)/Windows Kits/10/Include/10.0.22621.0/ucrt",
        "-IC:/Program Files (x86)/Windows Kits/10/Include/10.0.22621.0/shared",
        "-IC:/Program Files (x86)/Windows Kits/10/Include/10.0.22621.0/um",
        "-I../test/headers",
        "-fms-extensions"
    }

    add_links("$(projectdir)/lib/libclang.lib")
    add_includedirs("src", {public = true})
    add_headerfiles("src/**.hpp")
    local runargs = {"--input-directory", "$(projectdir)/test/headers", "--generated-directory", "$(projectdir)/generated/", "--filter", "src/minecraft"}
    for _, arg in ipairs(clang_args) do
        table.insert(runargs, arg)
    end
    set_runargs(runargs)
    set_rundir("$(projectdir)/build")