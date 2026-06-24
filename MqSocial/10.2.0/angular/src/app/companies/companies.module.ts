import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CommonModule } from '@angular/common';
import { CompaniesRoutingModule } from './companies-routing.module';
import { CompaniesComponent } from './companies.component';
import { CreateCompanyDialogComponent } from './create-company/create-company-dialog.component';
import { EditCompanyDialogComponent } from './edit-company/edit-company-dialog.component';

@NgModule({
    imports: [
        SharedModule,
        CommonModule,
        CompaniesRoutingModule,
        CompaniesComponent,
        CreateCompanyDialogComponent,
        EditCompanyDialogComponent,
    ],
})
export class CompaniesModule {}
