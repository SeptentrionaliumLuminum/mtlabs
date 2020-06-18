// Lab1.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <fstream>
#include <string>
#include <chrono>

#include <omp.h>

using namespace std;

int main(int argc, char* argv[])
{
    cout << "Hello, deadlock!\n";
	
	//READ FIRST MATRIX
	int** firstMatrix;

	int firstWidth;
	int firstHeight;

	FILE* input;
	fopen_s(&input, argv[1], "r");

	fseek(input, 0L, SEEK_SET);

	fscanf_s(input, "%d", &firstWidth);
	fscanf_s(input, "%d", &firstHeight);

	firstMatrix = new int* [firstHeight];
	for (int index = 0; index < firstHeight; index++)
		firstMatrix[index] = new int[firstWidth];

	for (int row = 0; row < firstHeight; row++)
		for (int column = 0; column < firstWidth; column++)
			fscanf_s(input, "%d", &firstMatrix[row][column]);

	//READ SECOND MATRIX
	int** secondMatrix;

	int secondWidth;
	int secondHeight;

	fopen_s(&input, argv[2], "r");

	fseek(input, 0L, SEEK_SET);

	fscanf_s(input, "%d", &secondWidth);
	fscanf_s(input, "%d", &secondHeight);

	secondMatrix = new int* [secondHeight];
	for (int index = 0; index < secondHeight; index++)
		secondMatrix[index] = new int[secondWidth];

	for (int row = 0; row < secondHeight; row++)
		for (int column = 0; column < secondWidth; column++)
			fscanf_s(input, "%d", &secondMatrix[row][column]);

	fclose(input);

	//Initialize result matrix
	long** result = new long* [firstHeight];
	for (int index = 0; index < firstHeight; index++)
		result[index] = new long[secondWidth];

	for (int row = 0; row < firstHeight; row++)
		for (int column = 0; column < secondWidth; column++)
			result[row][column] = 0;

	//Matrix multiplication

	int threads = atoi(argv[3]);

	omp_set_num_threads(threads);

	auto start = std::chrono::high_resolution_clock::now();

	int row;
	int column;
	int r;
	int index;

//#pragma omp parallel
//	{
//#pragma omp for private(row, column, r)
//		for (row = 0; row < firstHeight; row++)
//			for (column = 0; column < secondWidth; column++)
//				for (r = 0; r < firstWidth; r++)
//					result[row][column] = result[row][column] + firstMatrix[row][r] * secondMatrix[r][column];
//	}

#pragma omp parallel
	{
#pragma omp for private(row, column, r, index) schedule(static)
		for (index = 0; index < firstHeight * secondWidth; index++) 
		{
			row = index / secondWidth;
			column = index % secondWidth;
			for (r = 0; r < firstWidth; r++)
				result[row][column] = result[row][column] + firstMatrix[row][r] * secondMatrix[r][column];
		}
	}

	auto finish = std::chrono::high_resolution_clock::now();

	auto duration = std::chrono::duration_cast<std::chrono::microseconds>(finish - start).count();

	cout << duration;

	//Output
	ofstream fout;
	fout.open("output.txt");

	for (int row = 0; row < firstHeight; row++) 
	{
		for (int column = 0; column < secondWidth; column++) 
		{
			fout << result[row][column] << " ";
		}

		fout << "\n";
	}

	fout.close();
}