"""Minimal setup for ampel-display package."""

from setuptools import setup, find_packages

setup(
    name="ampel-display",
    version="1.0.0",
    description="Bogensport-Ampel LED Display for Raspberry Pi",
    package_dir={"": "src"},
    packages=find_packages(where="src"),
    python_requires=">=3.9",
    install_requires=[
        "pyserial>=3.5",
        "Pillow>=9.0",
    ],
    entry_points={
        "console_scripts": [
            "ampel-display=ampel_display.__main__:main",
        ],
    },
)
